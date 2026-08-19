using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Presentation.Api.Security;

public static class SecretComparison {
    public static bool FixedTimeEquals(string? expected, string? actual) {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual)) {
            return false;
        }

        byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        byte[] actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
