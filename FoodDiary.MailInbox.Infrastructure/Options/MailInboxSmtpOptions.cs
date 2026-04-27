namespace FoodDiary.MailInbox.Infrastructure.Options;

public sealed class MailInboxSmtpOptions {
    public const string SectionName = "MailInboxSmtp";

    public bool Enabled { get; init; } = true;

    public string ServerName { get; init; } = "mail.fooddiary.club";

    public int Port { get; init; } = 2525;

    public int MaxMessageSizeBytes { get; init; } = 10 * 1024 * 1024;

    public string[] AllowedRecipients { get; init; } = [
        "admin@fooddiary.club",
        "dmarc@fooddiary.club",
        "feedback@fooddiary.club",
        "support@fooddiary.club"
    ];

    public static bool HasValidConfiguration(MailInboxSmtpOptions options) {
        return options.Port > 0 &&
               options.MaxMessageSizeBytes > 0 &&
               !string.IsNullOrWhiteSpace(options.ServerName) &&
               options.AllowedRecipients.Length > 0 &&
               options.AllowedRecipients.All(static value => !string.IsNullOrWhiteSpace(value) && value.Contains('@', StringComparison.Ordinal));
    }
}
