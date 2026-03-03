using System.Text.Json.Serialization;

namespace BeerWallWeb.WxPay;

public record PrepayRequest(long OrderId);

// /v3/pay/transactions/jsapi 请求体
public class JsapiTransactionRequest
{
    [JsonPropertyName("appid")] public string AppId { get; set; } = "";
    [JsonPropertyName("mchid")] public string MchId { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("out_trade_no")] public string OutTradeNo { get; set; } = "";
    [JsonPropertyName("notify_url")] public string NotifyUrl { get; set; } = "";

    // ✅ 改名为 PayAmount / PayPayer，避免冲突
    [JsonPropertyName("amount")] public PayAmount Amount { get; set; } = new();
    [JsonPropertyName("payer")] public PayPayer Payer { get; set; } = new();

    public class PayAmount
    {
        [JsonPropertyName("total")] public int Total { get; set; } // 分
        [JsonPropertyName("currency")] public string Currency { get; set; } = "CNY";
    }

    public class PayPayer
    {
        [JsonPropertyName("openid")] public string OpenId { get; set; } = "";
    }
}

// /v3/pay/transactions/jsapi 响应体
public class JsapiTransactionResponse
{
    [JsonPropertyName("prepay_id")] public string PrepayId { get; set; } = "";
}

// 给小程序 wx.requestPayment 的参数
public class MiniProgramPayParams
{
    public string timeStamp { get; set; } = "";
    public string nonceStr { get; set; } = "";
    public string package { get; set; } = "";  // "prepay_id=xxx"
    public string signType { get; set; } = "RSA";
    public string paySign { get; set; } = "";
}

// 微信支付通知解密后（我们只用到交易状态/单号/out_trade_no）
public class WxPayNotifyDecrypted
{
    [JsonPropertyName("out_trade_no")] public string OutTradeNo { get; set; } = "";
    [JsonPropertyName("trade_state")] public string TradeState { get; set; } = ""; // SUCCESS 等
    [JsonPropertyName("transaction_id")] public string TransactionId { get; set; } = "";
    [JsonPropertyName("success_time")] public string? SuccessTime { get; set; } = null;
}

// 通知外层结构（含 resource）
public class WxPayNotifyEnvelope
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("create_time")] public string CreateTime { get; set; } = "";
    [JsonPropertyName("event_type")] public string EventType { get; set; } = "";
    [JsonPropertyName("resource_type")] public string ResourceType { get; set; } = "";
    [JsonPropertyName("resource")] public NotifyResource Resource { get; set; } = new();

    public class NotifyResource
    {
        [JsonPropertyName("algorithm")] public string Algorithm { get; set; } = "";
        [JsonPropertyName("ciphertext")] public string Ciphertext { get; set; } = "";
        [JsonPropertyName("associated_data")] public string AssociatedData { get; set; } = "";
        [JsonPropertyName("nonce")] public string Nonce { get; set; } = "";
    }
}