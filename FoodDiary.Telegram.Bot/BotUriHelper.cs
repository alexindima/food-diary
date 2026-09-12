namespace FoodDiary.Telegram.Bot;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class BotUriHelper {
    internal static bool TryCreateApiBaseUri(string? rawBaseUrl, out Uri? baseUri) {
        baseUri = null;
        if (string.IsNullOrWhiteSpace(rawBaseUrl)) {
            return false;
        }

        if (!Uri.TryCreate(rawBaseUrl.Trim(), UriKind.Absolute, out Uri? parsed) ||
            (!string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) &&
             !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)) ||
            parsed.UserInfo.Length != 0 || parsed.Query.Length != 0 || parsed.Fragment.Length != 0) {
            return false;
        }

        baseUri = parsed;
        return true;
    }

    internal static string? NormalizeWebAppUrl(string? rawWebAppUrl) {
        return string.IsNullOrWhiteSpace(rawWebAppUrl) ? null : rawWebAppUrl.TrimEnd('/');
    }
}
