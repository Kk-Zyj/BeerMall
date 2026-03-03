using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeerWallWeb.WxPay;

public class WxPayPlatformCertStore
{
    private readonly WxPayOptions _opt;
    private readonly WxPayHttpSigner _signer;
    private readonly IHttpClientFactory _httpClientFactory;

    // serial_no -> publicKeyPem
    private readonly ConcurrentDictionary<string, string> _pubKeyBySerial = new();
    private DateTime _lastRefreshUtc = DateTime.MinValue;

    public WxPayPlatformCertStore(WxPayOptions opt, WxPayHttpSigner signer, IHttpClientFactory httpClientFactory)
    {
        _opt = opt;
        _signer = signer;
        _httpClientFactory = httpClientFactory;
    }

    public bool TryGetPublicKeyPem(string serial, out string pem) => _pubKeyBySerial.TryGetValue(serial, out pem!);

    public async Task RefreshIfNeededAsync()
    {
        if ((DateTime.UtcNow - _lastRefreshUtc).TotalMinutes < _opt.PlatformCertRefreshMinutes && _pubKeyBySerial.Count > 0)
            return;

        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        // GET /v3/certificates
        const string path = "/v3/certificates";
        var auth = _signer.BuildAuthorization("GET", path, "");

        var client = _httpClientFactory.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "https://api.mch.weixin.qq.com" + path);
        req.Headers.TryAddWithoutValidation("Authorization", auth);
        req.Headers.TryAddWithoutValidation("Accept", "application/json");

        var resp = await client.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        resp.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<CertificatesResponse>(json);
        if (data?.Data == null) return;

        // 证书内容在 encrypt_certificate 里，需要用 APIv3Key AESGCM 解密
        foreach (var item in data.Data)
        {
            var enc = item.EncryptCertificate;
            var plain = WxPayCrypto.AesGcmDecryptToString(
                _opt.ApiV3Key,
                enc.AssociatedData,
                enc.Nonce,
                enc.Ciphertext
            );

            // plain 是平台证书 PEM（包含公钥）
            // 我们直接缓存整段证书 PEM 也可，这里缓存 PEM（ImportFromPem 支持证书/公钥）
            _pubKeyBySerial[item.SerialNo] = plain;
        }

        _lastRefreshUtc = DateTime.UtcNow;
    }

    private class CertificatesResponse
    {
        [JsonPropertyName("data")] public List<CertItem> Data { get; set; } = new();
    }

    private class CertItem
    {
        [JsonPropertyName("serial_no")] public string SerialNo { get; set; } = "";
        [JsonPropertyName("encrypt_certificate")] public EncryptCert EncryptCertificate { get; set; } = new();
    }

    private class EncryptCert
    {
        [JsonPropertyName("algorithm")] public string Algorithm { get; set; } = "";
        [JsonPropertyName("nonce")] public string Nonce { get; set; } = "";
        [JsonPropertyName("associated_data")] public string AssociatedData { get; set; } = "";
        [JsonPropertyName("ciphertext")] public string Ciphertext { get; set; } = "";
    }
}