namespace FoodDiary.Telegram.Bot;

internal static class BotUriHelper {
    internal static bool TryCreateApiBaseUri(string? rawBaseUrl, out Uri? baseUri) {
        baseUri = null;
        if (string.IsNullOrWhiteSpace(rawBaseUrl)) {
            return false;
        }

        if (!Uri.TryCreate(rawBaseUrl.Trim(), UriKind.Absolute, out Uri? parsed)) {
            return false;
        }

        baseUri = parsed;
        return true;
    }

    internal static string? NormalizeWebAppUrl(string? rawWebAppUrl) {
        return string.IsNullOrWhiteSpace(rawWebAppUrl) ? null : rawWebAppUrl.TrimEnd('/');
    }
}
