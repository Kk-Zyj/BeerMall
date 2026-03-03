using BeerMall.Api.Data;
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

    public WxPayService(
        BeerMallContext db,
        WxPayOptions opt,
        WxPayHttpSigner signer,
        WxPayPlatformCertStore certStore,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _opt = opt;
        _signer = signer;
        _certStore = certStore;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<MiniProgramPayParams> CreateJsapiPrepayAsync(long orderId)
    {
        // 1) 查订单
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) throw new Exception("订单不存在");
        if (order.Status != 0) throw new Exception("订单状态不正确");

        // 2) 查用户 openid（你项目里 User.OpenId）
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.OpenId)) throw new Exception("用户openid缺失");

        // 3) 金额（分）
        var totalFen = (int)Math.Round(order.TotalAmount * 100m, 0, MidpointRounding.AwayFromZero);
        if (totalFen <= 0) totalFen = 1;

        var reqBody = new JsapiTransactionRequest
        {
            AppId = _opt.AppId,
            MchId = _opt.MchId,
            Description = "啤酒商城订单",
            OutTradeNo = order.OrderNo,          // 建议 OrderNo 唯一
            NotifyUrl = _opt.NotifyUrl,
            Amount = new JsapiTransactionRequest.PayAmount { Total = totalFen, Currency = "CNY" },
            Payer = new JsapiTransactionRequest.PayPayer { OpenId = user.OpenId }
        };

        var bodyJson = JsonSerializer.Serialize(reqBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        const string path = "/v3/pay/transactions/jsapi";

        var auth = _signer.BuildAuthorization("POST", path, bodyJson);

        var client = _httpClientFactory.CreateClient();
        var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.mch.weixin.qq.com" + path);
        httpReq.Headers.TryAddWithoutValidation("Authorization", auth);
        httpReq.Headers.TryAddWithoutValidation("Accept", "application/json");
        httpReq.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var resp = await client.SendAsync(httpReq);
        var respJson = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();

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
            return;

        // 7) 按 out_trade_no 找订单并更新状态（幂等：只有 status=0 才更新）
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNo == dec.OutTradeNo);
        if (order == null) return;

        if (order.Status == 0)
        {
            // 普通订单：待发货(1)；拼团订单：待成团(10) 或你现有逻辑
            order.Status = (order.OrderType != 0) ? 10 : 1;

            // 建议加字段保存微信单号/成功时间（见下文“字段建议”）
            // order.WxTransactionId = dec.TransactionId;
            // order.PaidTime = DateTime.Parse(dec.SuccessTime ?? DateTime.UtcNow.ToString("o"));

            await _db.SaveChangesAsync();
        }
    }
}