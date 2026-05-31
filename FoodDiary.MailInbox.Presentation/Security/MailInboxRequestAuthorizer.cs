using FoodDiary.MailInbox.Presentation.Options;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.MailInbox.Presentation.Security;

internal static class MailInboxRequestAuthorizer {
    public static bool IsAuthorized(HttpRequest request, MailInboxHttpOptions options) {
        if (!request.Headers.TryGetValue("X-MailInbox-Api-Key", out var values)) {
            return false;
        }

        return options.RequireApiKey &&
               !string.IsNullOrWhiteSpace(options.ApiKey) &&
               string.Equals(values.ToString(), options.ApiKey, StringComparison.Ordinal);
    }
}
