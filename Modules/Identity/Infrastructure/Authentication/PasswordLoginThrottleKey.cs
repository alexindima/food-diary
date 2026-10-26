using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Modules.Identity.Infrastructure.Authentication;

internal static class PasswordLoginThrottleKey {
    public static string FromEmail(string email) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToUpperInvariant())));
}
