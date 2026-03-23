using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerMall.Api.Services;
using BeerWallWeb.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace BeerWallWeb.WxPay;

public class WxPayService
{
    private readonly BeerMallContext _db;
    private readonly WxPayOptions _opt;
    private readonly WxPayHttpSigner _signer;
    private readonly WxPayPlatformCertStore _certStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RiskControlService _riskService;
    private readonly ILogger<WxPayService> _logger;

    public WxPayService(
        BeerMallContext db,
        WxPayOptions opt,
        WxPayHttpSigner signer,
        WxPayPlatformCertStore certStore,
        IHttpClientFactory httpClientFactory,
        RiskControlService riskService,
        ILogger<WxPayService> logger)
    {
        _db = db;
        _opt = opt;
        _signer = signer;
        _certStore = certStore;
        _httpClientFactory = httpClientFactory;
        _riskService = riskService;
        _logger = logger;
    }

    public async Task<MiniProgramPayParams> CreateJsapiPrepayAsync(long orderId, long userId)
    {
        ValidatePrepayConfig();

        // 1) 查订单并校验归属
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) throw new Exception("订单不存在或无权操作");
        if (order.Status != 0) throw new Exception("订单状态不正确，无法发起支付");

        // 2) 查用户 openid
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.OpenId))
            throw new Exception("用户openid缺失");

        // 3) 金额（分）
        var totalFen = (int)Math.Round(order.TotalAmount * 100m, 0, MidpointRounding.AwayFromZero);
        if (totalFen <= 0) totalFen = 1;

        var reqBody = new JsapiTransactionRequest
        {
            AppId = _opt.AppId,
            MchId = _opt.MchId,
            Description = $"啤酒商城订单 {order.OrderNo}",
            OutTradeNo = order.OrderNo,
            NotifyUrl = _opt.NotifyUrl,
            Amount = new JsapiTransactionRequest.PayAmount
            {
                Total = totalFen,
                Currency = "CNY"
            },
            Payer = new JsapiTransactionRequest.PayPayer
            {
                OpenId = user.OpenId
            }
        };

        var bodyJson = JsonSerializer.Serialize(reqBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        const string path = "/v3/pay/transactions/jsapi";

        var auth = _signer.BuildAuthorization("POST", path, bodyJson);

        var client = _httpClientFactory.CreateClient();
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.mch.weixin.qq.com" + path);
        httpReq.Headers.TryAddWithoutValidation("Authorization", auth);
        httpReq.Headers.TryAddWithoutValidation("Accept", "application/json");
        httpReq.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var resp = await client.SendAsync(httpReq);
        var respJson = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("微信下单失败，HTTP={StatusCode}，Body={Body}", (int)resp.StatusCode, respJson);
            throw new Exception("微信预支付下单失败");
        }

        var prepay = JsonSerializer.Deserialize<JsapiTransactionResponse>(respJson);
        if (prepay == null || string.IsNullOrWhiteSpace(prepay.PrepayId))
            throw new Exception("获取prepay_id失败");

        // 4) 生成小程序支付参数签名
        var timeStamp = WxPayCrypto.UnixTimeSeconds().ToString();
        var nonceStr = WxPayCrypto.NonceStr();
        var pkg = "prepay_id=" + prepay.PrepayId;

        // paySign 签名串：appid\ntimestamp\nnonceStr\npackage\n
        var paySignMessage = $"{_opt.AppId}\n{timeStamp}\n{nonceStr}\n{pkg}\n";
        var paySign = WxPayCrypto.Sha256WithRsaSignBase64(paySignMessage, _opt.MerchantPrivateKeyPem);

        return new MiniProgramPayParams
        {
            timeStamp = timeStamp,
            nonceStr = nonceStr,
            package = pkg,
            signType = "RSA",
            paySign = paySign
        };
    }

    public async Task HandleNotifyAsync(HttpRequest request)
    {
        // 1) 取微信签名头
        var serial = request.Headers["Wechatpay-Serial"].ToString();
        var signature = request.Headers["Wechatpay-Signature"].ToString();
        var timestamp = request.Headers["Wechatpay-Timestamp"].ToString();
        var nonce = request.Headers["Wechatpay-Nonce"].ToString();

        if (string.IsNullOrWhiteSpace(serial) || string.IsNullOrWhiteSpace(signature))
            throw new Exception("缺少微信回调验签头");

        // 2) 读 body
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        // 3) 刷新平台证书并拿公钥
        await _certStore.RefreshIfNeededAsync();
        if (!_certStore.TryGetPublicKeyPem(serial, out var platformPem))
            throw new Exception("平台证书未就绪，无法验签");

        // 4) 验签串：timestamp\nnonce\nbody\n
        var message = $"{timestamp}\n{nonce}\n{body}\n";
        var ok = WxPayCrypto.VerifySha256WithRsa(message, signature, platformPem);
        if (!ok) throw new Exception("微信回调验签失败");

        // 5) 解密 resource
        var env = JsonSerializer.Deserialize<WxPayNotifyEnvelope>(body);
        if (env?.Resource == null) throw new Exception("回调内容为空");

        var plain = WxPayCrypto.AesGcmDecryptToString(
            _opt.ApiV3Key,
            env.Resource.AssociatedData,
            env.Resource.Nonce,
            env.Resource.Ciphertext
        );

        var dec = JsonSerializer.Deserialize<WxPayNotifyDecrypted>(plain);
        if (dec == null) throw new Exception("回调解密内容解析失败");

        // 6) 只处理 SUCCESS
        if (!string.Equals(dec.TradeState, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("微信支付回调非SUCCESS，OutTradeNo={OutTradeNo}, TradeState={TradeState}",
                dec.OutTradeNo, dec.TradeState);
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // 7) 查订单
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNo == dec.OutTradeNo);

        if (order == null)
        {
            _logger.LogWarning("支付回调未找到订单，OutTradeNo={OutTradeNo}", dec.OutTradeNo);
            await transaction.CommitAsync();
            return;
        }

        // 8) 幂等处理：只允许从 0 -> 已支付状态
        var nextStatus = (order.OrderType != 0) ? 10 : 1;

        var updated = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Orders
            SET Status = {nextStatus}
            WHERE Id = {order.Id} AND Status = 0;
        ");

        if (updated <= 0)
        {
            _logger.LogInformation("订单已处理过或状态不允许重复处理，OrderId={OrderId}, OrderNo={OrderNo}, Status={Status}",
                order.Id, order.OrderNo, order.Status);
            await transaction.CommitAsync();
            return;
        }

        order.Status = nextStatus;

        // 9) 支付成功后的业务后置处理
        await ProcessPaidOrderAsync(order);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation("支付回调处理成功，OrderId={OrderId}, OrderNo={OrderNo}, Status={Status}",
            order.Id, order.OrderNo, order.Status);
    }

    private async Task ProcessPaidOrderAsync(Order order)
    {
        if (order.OrderType != 0)
        {
            await HandleGroupOrderPaidAsync(order);
            return;
        }

        await HandleNormalOrderPaidAsync(order);
    }

    private async Task HandleGroupOrderPaidAsync(Order order)
    {
        if (!order.GroupBuyId.HasValue)
        {
            _logger.LogWarning("拼团订单缺少 GroupBuyId，OrderId={OrderId}", order.Id);
            return;
        }

        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE GroupBuyInstances
            SET CurrentCount = CurrentCount + 1
            WHERE Id = {order.GroupBuyId.Value}
              AND Status = 0
              AND CurrentCount < TargetCount
              AND ExpireTime > NOW();
        ");

        var instance = await _db.GroupBuyInstances.FirstOrDefaultAsync(g => g.Id == order.GroupBuyId.Value);
        if (instance == null)
        {
            _logger.LogWarning("未找到拼团实例，GroupBuyId={GroupBuyId}, OrderId={OrderId}", order.GroupBuyId.Value, order.Id);
            return;
        }

        if (affected <= 0)
        {
            // 这里不抛异常，避免微信无限重试；保留订单为 10，后续可由过期任务/人工处理
            _logger.LogWarning(
                "拼团计数未成功更新，GroupBuyId={GroupBuyId}, OrderId={OrderId}, GroupStatus={GroupStatus}, CurrentCount={CurrentCount}, TargetCount={TargetCount}, ExpireTime={ExpireTime}",
                instance.Id, order.Id, instance.Status, instance.CurrentCount, instance.TargetCount, instance.ExpireTime);
            return;
        }

        // 重新读取最新人数
        await _db.Entry(instance).ReloadAsync();

        if (instance.Status == 0 && instance.CurrentCount >= instance.TargetCount)
        {
            instance.Status = 1;

            var waitingOrders = await _db.Orders
                .Where(o => o.GroupBuyId == instance.Id && o.Status == 10)
                .ToListAsync();

            foreach (var item in waitingOrders)
            {
                item.Status = 1;
            }

            _logger.LogInformation("拼团成功，GroupBuyId={GroupBuyId}, TotalOrders={Count}", instance.Id, waitingOrders.Count);
        }
    }

    private async Task HandleNormalOrderPaidAsync(Order order)
    {
        // A. 创建裂变任务（幂等）
        if (order.TotalAmount > 0)
        {
            bool hasTask = await _db.FissionTasks.AnyAsync(t => t.OrderId == order.Id);
            if (!hasTask)
            {
                var task = new FissionTask
                {
                    InitiatorId = order.UserId,
                    OrderId = order.Id,
                    SourceOrderAmount = order.TotalAmount,
                    TargetThreshold = order.TotalAmount - 5,
                    TargetCount = 3,
                    Status = 0,
                    ExpireTime = DateTime.Now.AddDays(3),
                    ParticipantLog = "[]"
                };
                _db.FissionTasks.Add(task);
            }
        }

        // B. 如果买家有邀请人，则尝试计入助力
        var buyer = await _db.Users.FindAsync(order.UserId);
        if (buyer != null && buyer.InviterId.HasValue)
        {
            await ProcessHelpLogic(buyer.InviterId.Value, order);
        }
    }

    private async Task ProcessHelpLogic(long initiatorId, Order currentOrder)
    {
        var task = await _db.FissionTasks
            .FirstOrDefaultAsync(t => t.InitiatorId == initiatorId && t.Status == 0);

        if (task == null) return;

        if (DateTime.Now > task.ExpireTime)
        {
            task.Status = -1;
            return;
        }

        if (currentOrder.UserId == initiatorId) return;

        if (currentOrder.TotalAmount < task.TargetThreshold) return;

        var participants = JsonSerializer.Deserialize<List<long>>(task.ParticipantLog) ?? new List<long>();
        if (participants.Contains(currentOrder.UserId)) return;

        var riskCheck = await _riskService.CheckRiskAsync(currentOrder.UserId, currentOrder.DeviceId);
        if (!riskCheck.Pass)
        {
            currentOrder.RiskStatus = 1;
            currentOrder.RiskReason = riskCheck.Reason;
            return;
        }

        participants.Add(currentOrder.UserId);
        task.ParticipantLog = JsonSerializer.Serialize(participants);
        task.CurrentCount++;
        currentOrder.ParentTaskId = task.Id;

        if (task.CurrentCount >= task.TargetCount)
        {
            task.Status = 1;
        }
    }

    private void ValidatePrepayConfig()
    {
        if (string.IsNullOrWhiteSpace(_opt.AppId)) throw new Exception("WxPay:AppId 未配置");
        if (string.IsNullOrWhiteSpace(_opt.MchId)) throw new Exception("WxPay:MchId 未配置");
        if (string.IsNullOrWhiteSpace(_opt.NotifyUrl)) throw new Exception("WxPay:NotifyUrl 未配置");
        if (string.IsNullOrWhiteSpace(_opt.MerchantPrivateKeyPem)) throw new Exception("WxPay:MerchantPrivateKeyPem 未配置");
        if (string.IsNullOrWhiteSpace(_opt.ApiV3Key)) throw new Exception("WxPay:ApiV3Key 未配置");
    }
}