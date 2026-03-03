namespace BeerWallWeb.WxPay;

public class WxPayOptions
{
    // 小程序 AppId（你已有）
    public string AppId { get; set; } = "";

    // 微信支付商户号 mchid（你之后拿到）
    public string MchId { get; set; } = "";

    // 商户证书序列号 serial_no（你之后拿到）
    public string MerchantSerialNo { get; set; } = "";

    // 商户私钥（apiclient_key.pem 内容）
    // 建议以“文件路径”或“环境变量存 PEM 内容”的方式提供
    public string MerchantPrivateKeyPem { get; set; } = "";

    // APIv3Key（32位字符串）
    public string ApiV3Key { get; set; } = "";

    // 支付回调 notify_url（必须公网 HTTPS）
    public string NotifyUrl { get; set; } = "";

    // 平台证书缓存刷新间隔（分钟）
    public int PlatformCertRefreshMinutes { get; set; } = 60;
}