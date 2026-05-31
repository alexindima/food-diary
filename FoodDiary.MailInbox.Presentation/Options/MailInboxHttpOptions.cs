namespace FoodDiary.MailInbox.Presentation.Options;

public sealed class MailInboxHttpOptions {
    public const string SectionName = "MailInboxHttp";

    public bool RequireApiKey { get; init; } = true;
    public string ApiKey { get; init; } = string.Empty;

    public static bool HasValidApiKey(MailInboxHttpOptions options) {
        return options.RequireApiKey && !string.IsNullOrWhiteSpace(options.ApiKey);
    }
}
