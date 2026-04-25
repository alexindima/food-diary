namespace FoodDiary.MailInbox.Client.Options;

public sealed class MailInboxClientOptions {
    public const string SectionName = "MailInboxClient";

    public string BaseUrl { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    public static bool HasValidBaseUrl(MailInboxClientOptions options) =>
        Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute);
}
