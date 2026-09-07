namespace FoodDiary.MailRelay.Client.Options;

public sealed class MailRelayClientOptions {
    public const string SectionName = "MailRelayClient";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
    public bool AllowInsecureHttp { get; set; }

    public static bool HasValidBaseUrl(MailRelayClientOptions options) {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment)) {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
               (options.AllowInsecureHttp &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }
}
