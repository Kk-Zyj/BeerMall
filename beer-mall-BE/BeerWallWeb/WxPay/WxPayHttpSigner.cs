using System.Text;
using System.Text.Json;

namespace BeerWallWeb.WxPay;

public class WxPayHttpSigner
{
    private readonly WxPayOptions _opt;

    public WxPayHttpSigner(WxPayOptions opt) => _opt = opt;

    // 生成 v3 API Authorization 头
    public string BuildAuthorization(string method, string urlPathWithQuery, string bodyJson)
    {
        // message = HTTPMethod \n URL \n timestamp \n nonce \n body \n
        var timestamp = WxPayCrypto.UnixTimeSeconds().ToString();
        var nonceStr = WxPayCrypto.NonceStr();

        var message = $"{method}\n{urlPathWithQuery}\n{timestamp}\n{nonceStr}\n{bodyJson}\n";
        var signature = WxPayCrypto.Sha256WithRsaSignBase64(message, _opt.MerchantPrivateKeyPem);

        // WECHATPAY2-SHA256-RSA2048 mchid="xxx",nonce_str="xxx",signature="xxx",timestamp="xxx",serial_no="xxx"
        return $"WECHATPAY2-SHA256-RSA2048 mchid=\"{_opt.MchId}\",nonce_str=\"{nonceStr}\",signature=\"{signature}\",timestamp=\"{timestamp}\",serial_no=\"{_opt.MerchantSerialNo}\"";
    }

    public static string ToJson(object obj) =>
        JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = null });
}