namespace FoodDiary.Web.Api.Options;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ApiCorsOptions {
    public const string SectionName = "Cors";

    public string[] Origins { get; init; } = [];

    public static bool HasValidOrigins(ApiCorsOptions options) {
        if (options.Origins.Length == 0) {
            return false;
        }

        var canonicalOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string origin in options.Origins) {
            if (!IsCanonicalHttpOrigin(origin, out string canonicalOrigin) ||
                !canonicalOrigins.Add(canonicalOrigin)) {
                return false;
            }
        }

        return true;
    }

    private static bool IsCanonicalHttpOrigin(string? origin, out string canonicalOrigin) {
        canonicalOrigin = string.Empty;
        if (string.IsNullOrWhiteSpace(origin) ||
            !Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) ||
            !IsHttpScheme(uri.Scheme) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment)) {
            return false;
        }

        canonicalOrigin = uri.GetLeftPart(UriPartial.Authority);
        return string.Equals(origin, canonicalOrigin, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpScheme(string scheme) =>
        string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
