using Coherent.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Coherent.Infrastructure.Services;

/// <summary>
/// ADHICS Compliance: AES-256 encryption for data at rest
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _iv;
    private readonly string _legacyPasswordToken;

    public EncryptionService(IConfiguration configuration)
    {
        var key = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption key not configured");
        var iv = configuration["Encryption:IV"] ?? throw new InvalidOperationException("Encryption IV not configured");
        _legacyPasswordToken = configuration["LegacyAuth:PasswordToken"] ?? "pragmedic";
        
        _encryptionKey = Convert.FromBase64String(key);
        _iv = Convert.FromBase64String(iv);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string GenerateSecurityKey()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashSecurityKey(string securityKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(securityKey));
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifySecurityKey(string securityKey, string securityKeyHash)
    {
        var computedHash = HashSecurityKey(securityKey);
        return computedHash == securityKeyHash;
    }

    public string LegacyEncryptPassword(string plainText, string token)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Legacy password token is required", nameof(token));

        using var des = new TripleDESCryptoServiceProvider();
        des.IV = new byte[8];

#pragma warning disable SYSLIB0023
        using var pdb = new PasswordDeriveBytes(token, Array.Empty<byte>());
        des.Key = pdb.CryptDeriveKey("RC2", "MD5", 128, new byte[8]);
#pragma warning restore SYSLIB0023

        var plainBytes = Encoding.UTF8.GetBytes(plainText);

        using var ms = new MemoryStream((plainBytes.Length * 2) - 1);
        using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(plainBytes, 0, plainBytes.Length);
            cs.FlushFinalBlock();
        }

        var encryptedBytes = ms.ToArray();
        return Convert.ToBase64String(encryptedBytes);
    }

    public string LegacyDecryptPassword(string cipherText, string token)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (string.IsNullOrEmpty(token))
            throw new ArgumentException("Legacy password token is required", nameof(token));

        using var des = new TripleDESCryptoServiceProvider();
        des.IV = new byte[8];

#pragma warning disable SYSLIB0023
        using var pdb = new PasswordDeriveBytes(token, Array.Empty<byte>());
        des.Key = pdb.CryptDeriveKey("RC2", "MD5", 128, new byte[8]);
#pragma warning restore SYSLIB0023

        var encryptedBytes = Convert.FromBase64String(cipherText);

        using var ms = new MemoryStream(cipherText.Length);
        using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(encryptedBytes, 0, encryptedBytes.Length);
            cs.FlushFinalBlock();
        }

        var plainBytes = ms.ToArray();
        return Encoding.UTF8.GetString(plainBytes);
    }
}
