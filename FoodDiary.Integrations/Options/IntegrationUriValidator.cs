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

    public static bool IsVapidSubject(string? value) {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            !string.IsNullOrEmpty(uri.Fragment)) {
            return false;
        }

        if (string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase)) {
            int schemeSeparator = value!.IndexOf(':', StringComparison.Ordinal);
            string address = schemeSeparator >= 0 ? value[(schemeSeparator + 1)..] : string.Empty;
            return System.Net.Mail.MailAddress.TryCreate(address, out _);
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(uri.Host) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }

    public static bool IsSafeNavigationUrl(string? value) {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)) {
            return false;
        }

        if (value[0] == '/') {
            return value.Length == 1 || (value[1] is not ('/' or '\\'));
        }

        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
               string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
               !string.IsNullOrWhiteSpace(uri.Host) &&
               string.IsNullOrEmpty(uri.UserInfo);
    }
}
