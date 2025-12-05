namespace Coherent.Core.Interfaces;

/// <summary>
/// ADHICS Compliance: AES-256 encryption service for data at rest
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
    string GenerateSecurityKey();
    string HashSecurityKey(string securityKey);
    bool VerifySecurityKey(string securityKey, string securityKeyHash);
}
