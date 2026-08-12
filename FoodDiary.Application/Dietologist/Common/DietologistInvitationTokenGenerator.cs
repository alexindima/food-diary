using System.Security.Cryptography;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistInvitationTokenGenerator {
    public static string GenerateUrlSafeToken(int byteLength = 32) {
        if (byteLength <= 0) {
            throw new ArgumentOutOfRangeException(nameof(byteLength), byteLength, "Byte length must be greater than zero.");
        }

        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
