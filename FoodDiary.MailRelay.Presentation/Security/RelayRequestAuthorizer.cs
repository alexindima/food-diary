using Microsoft.AspNetCore.Http;

namespace FoodDiary.MailRelay.Presentation.Security;

internal static class RelayRequestAuthorizer {
    public static bool IsAuthorized(HttpRequest request, MailRelayOptions options) {
        if (!request.Headers.TryGetValue("X-Relay-Api-Key", out var values)) {
            return false;
        }

        return options.RequireApiKey &&
               !string.IsNullOrWhiteSpace(options.ApiKey) &&
               string.Equals(values.ToString(), options.ApiKey, StringComparison.Ordinal);
    }
}
