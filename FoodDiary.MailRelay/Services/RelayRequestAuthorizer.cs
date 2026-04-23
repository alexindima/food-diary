using FoodDiary.MailRelay.Options;

namespace FoodDiary.MailRelay.Services;

internal static class RelayRequestAuthorizer {
    public static bool IsAuthorized(HttpRequest request, MailRelayOptions options) {
        if (!options.RequireApiKey) {
            return true;
        }

        if (!request.Headers.TryGetValue("X-Relay-Api-Key", out var values)) {
            return false;
        }

        return string.Equals(values.ToString(), options.ApiKey, StringComparison.Ordinal);
    }
}
