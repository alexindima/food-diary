namespace FoodDiary.Integrations.Options;

internal static class IntegrationUriValidator {
    public static bool IsAbsoluteHttpsBaseUrl(string? value) =>
        IsAbsoluteHttpBaseUrl(value, requireHttps: true);

    public static bool IsAbsoluteHttpBaseUrl(string? value, bool requireHttps = false) {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment)) {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               (!requireHttps && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsSecureRedirectUrl(string? value) {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Fragment)) {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && uri.IsLoopback);
    }

}
