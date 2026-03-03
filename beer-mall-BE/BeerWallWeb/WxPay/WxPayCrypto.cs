using System.Security.Cryptography;
using System.Text;

namespace BeerWallWeb.WxPay;

public static class WxPayCrypto
{
    public static string Sha256WithRsaSignBase64(string message, string merchantPrivateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(merchantPrivateKeyPem);

        var data = Encoding.UTF8.GetBytes(message);
        var sign = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(sign);
    }

    public static bool VerifySha256WithRsa(string message, string signatureBase64, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var data = Encoding.UTF8.GetBytes(message);
        var sign = Convert.FromBase64String(signatureBase64);
        return rsa.VerifyData(data, sign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    // 微信支付通知 resource 解密：AES-256-GCM
    public static string AesGcmDecryptToString(string apiV3Key, string associatedData, string nonce, string ciphertextBase64)
    {
        // ciphertext = base64(密文 + 16字节tag)
        var cipherBytes = Convert.FromBase64String(ciphertextBase64);
        var keyBytes = Encoding.UTF8.GetBytes(apiV3Key);
        var nonceBytes = Encoding.UTF8.GetBytes(nonce);
        var aadBytes = Encoding.UTF8.GetBytes(associatedData);

        var tag = cipherBytes[^16..];
        var cipher = cipherBytes[..^16];
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(keyBytes);
        aes.Decrypt(nonceBytes, cipher, tag, plain, aadBytes);

        return Encoding.UTF8.GetString(plain);
    }

    public static string NonceStr(int len = 32)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = RandomNumberGenerator.GetBytes(len);
        var sb = new StringBuilder(len);
        for (int i = 0; i < len; i++) sb.Append(chars[bytes[i] % chars.Length]);
        return sb.ToString();
    }

    public static long UnixTimeSeconds() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}