namespace FoodDiary.Infrastructure.Integrations.BugTriage;

public sealed class AdminBugTriageOptions {
    public string BaseUrl { get; set; } = string.Empty;
    public string ReadApiKey { get; set; } = string.Empty;
    public bool AllowInsecureLoopback { get; set; }

    public static bool IsValid(AdminBugTriageOptions options) {
        if (options.BaseUrl.Length == 0 && options.ReadApiKey.Length == 0) { return true; }
        return options.ReadApiKey.Length is >= 32 and <= 256 && Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) &&
            uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) ||
                (options.AllowInsecureLoopback && uri.IsLoopback && string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal)));
    }
}
