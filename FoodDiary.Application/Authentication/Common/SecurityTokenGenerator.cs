using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Application.Authentication.Common;

public static class SecurityTokenGenerator {
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
}
