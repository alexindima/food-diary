using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Presentation.Api.Security;

public static class SecretComparison {
    public static bool FixedTimeEquals(string? expected, string? actual) {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual)) {
            return false;
        }

        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] actualBytes = Encoding.UTF8.GetBytes(actual);

        return expectedBytes.Length == actualBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
