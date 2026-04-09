using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Application.Authentication.Common;

public static class SecurityTokenGenerator {
    private const string Sha256StoragePrefix = "sha256:";

    public static string GenerateUrlSafeToken(int byteLength = 32) {
        if (byteLength <= 0) {
            throw new ArgumentOutOfRangeException(nameof(byteLength), byteLength, "Byte length must be greater than zero.");
        }

        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public static string NormalizeForSecureHashing(string token) {
        if (string.IsNullOrWhiteSpace(token)) {
            throw new ArgumentException("Token is required.", nameof(token));
        }

        var normalizedToken = token.Trim();
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedToken));
        return Convert.ToHexString(digest);
    }

    public static string HashForStorage(string token) {
        return $"{Sha256StoragePrefix}{NormalizeForSecureHashing(token)}";
    }

    public static bool IsFastStorageHash(string storedHash) {
        return !string.IsNullOrWhiteSpace(storedHash) &&
               storedHash.StartsWith(Sha256StoragePrefix, StringComparison.Ordinal);
    }

    public static bool VerifyFastStorageHash(string token, string storedHash) {
        if (!IsFastStorageHash(storedHash)) {
            return false;
        }

        var expectedHash = HashForStorage(token);
        var storedBytes = Encoding.UTF8.GetBytes(storedHash);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedHash);
        return CryptographicOperations.FixedTimeEquals(storedBytes, expectedBytes);
    }
}
