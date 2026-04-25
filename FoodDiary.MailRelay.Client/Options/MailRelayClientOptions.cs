namespace FoodDiary.MailRelay.Client.Options;

public sealed class MailRelayClientOptions {
    public const string SectionName = "MailRelayClient";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    public static bool HasValidBaseUrl(MailRelayClientOptions options) =>
        Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute);
}
