namespace FoodDiary.MailInbox.Client.Options;

public sealed class MailInboxClientOptions {
    public const string SectionName = "MailInboxClient";
    public const int MinApiKeyLength = 32;
    public const int MaxApiKeyLength = 256;
    private static readonly HashSet<string> KnownInsecureApiKeys = new(StringComparer.Ordinal) {
        "0123456789abcdef0123456789abcdea",
        "0123456789abcdef0123456789abcdeb",
        "0123456789abcdef0123456789abcdec",
    };

    public string BaseUrl { get; set; } = string.Empty;
    public string MetadataApiKey { get; set; } = string.Empty;
    public string ContentApiKey { get; set; } = string.Empty;
    public string StateApiKey { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
    public bool AllowInsecureLoopback { get; set; }

    public static bool HasValidBaseUrl(MailInboxClientOptions options) {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment)) {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) ||
               (options.AllowInsecureLoopback &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal) &&
                uri.IsLoopback);
    }

    public static bool HasValidApiKey(MailInboxClientOptions options) =>
        HasValidKey(options.MetadataApiKey) &&
        HasValidKey(options.ContentApiKey) &&
        HasValidKey(options.StateApiKey) &&
        !string.Equals(options.MetadataApiKey, options.ContentApiKey, StringComparison.Ordinal) &&
        !string.Equals(options.MetadataApiKey, options.StateApiKey, StringComparison.Ordinal) &&
        !string.Equals(options.ContentApiKey, options.StateApiKey, StringComparison.Ordinal);

    private static bool HasValidKey(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length is >= MinApiKeyLength and <= MaxApiKeyLength &&
        !KnownInsecureApiKeys.Contains(value);
}
