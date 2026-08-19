using System.Security.Cryptography;
using System.Text;
using FoodDiary.MailInbox.Presentation.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace FoodDiary.MailInbox.Presentation.Security;

internal static class MailInboxRequestAuthorizer {
    public static bool IsAuthorized(HttpRequest request, MailInboxHttpOptions options) {
        if (!options.RequireApiKey ||
            string.IsNullOrWhiteSpace(options.ApiKey) ||
            !request.Headers.TryGetValue("X-MailInbox-Api-Key", out StringValues values) ||
            values.Count != 1 ||
            string.IsNullOrEmpty(values[0])) {
            return false;
        }

        byte[] expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(options.ApiKey));
        byte[] suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(values[0]!));
        return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
    }
}
