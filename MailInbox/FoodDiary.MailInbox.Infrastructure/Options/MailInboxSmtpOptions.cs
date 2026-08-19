using FoodDiary.MailInbox.Infrastructure.Services;

namespace FoodDiary.MailInbox.Infrastructure.Options;

public sealed class MailInboxSmtpOptions {
    public const string SectionName = "MailInboxSmtp";

    public bool Enabled { get; init; } = true;

    public string ServerName { get; init; } = "mail.fooddiary.club";

    public int Port { get; init; } = 2525;

    public string CertificatePath { get; init; } = string.Empty;

    public string PrivateKeyPath { get; init; } = string.Empty;

    public int MaxMessageSizeBytes { get; init; } = 10 * 1024 * 1024;

    public int MaxConcurrentConnections { get; init; } = 32;

    public int MaxConcurrentConnectionsPerIp { get; init; } = 4;

    public int MaxConcurrentMessageProcessing { get; init; } = 4;

    public TimeSpan ProcessingQueueTimeout { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan SessionTimeout { get; init; } = TimeSpan.FromMinutes(2);

    public int MaxMessagesPerSession { get; init; } = 10;

    public int MaxMessagesPerIpPerHour { get; init; } = 100;

    public int MaxMessagesPerSenderPerHour { get; init; } = 20;

    public long MaxRawBytesPerIpPerHour { get; init; } = 64L * 1024 * 1024;

    public int MaxTrackedRateLimitKeys { get; init; } = 10_000;

    public int MaxRecipientsPerMessage { get; init; } = 4;

    public int MaxMimeParts { get; init; } = 100;

    public int MaxMimeDepth { get; init; } = 20;

    public int MaxExtractedBodyCharacters { get; init; } = 1_000_000;

    public string[] AllowedRecipients { get; init; } = [
        "admin@fooddiary.club",
        "dmarc@fooddiary.club",
        "feedback@fooddiary.club",
        "support@fooddiary.club",
    ];

    public static bool HasValidConfiguration(MailInboxSmtpOptions options) {
        return options is {
            Port: > 0,
            MaxMessageSizeBytes: > 0,
            MaxConcurrentConnections: > 0,
            MaxConcurrentConnectionsPerIp: > 0,
            MaxConcurrentMessageProcessing: > 0,
            MaxMessagesPerSession: > 0,
            MaxMessagesPerIpPerHour: > 0,
            MaxMessagesPerSenderPerHour: > 0,
            MaxRawBytesPerIpPerHour: > 0,
            MaxTrackedRateLimitKeys: > 0,
            MaxRecipientsPerMessage: > 0,
            MaxMimeParts: > 0,
            MaxMimeDepth: > 0,
            MaxExtractedBodyCharacters: > 0,
        } &&
               options.MaxConcurrentConnectionsPerIp <= options.MaxConcurrentConnections &&
               options.MaxRawBytesPerIpPerHour >= options.MaxMessageSizeBytes &&
               options.MaxRecipientsPerMessage <= MailInboxStoredMessageLimits.MaxRecipients &&
               options.ProcessingQueueTimeout > TimeSpan.Zero &&
               options.SessionTimeout > TimeSpan.Zero &&
               !string.IsNullOrWhiteSpace(options.ServerName) &&
               (!options.Enabled ||
                (!string.IsNullOrWhiteSpace(options.CertificatePath) &&
                 !string.IsNullOrWhiteSpace(options.PrivateKeyPath))) &&
               options.AllowedRecipients.Length > 0 &&
               options.AllowedRecipients.All(static value => !string.IsNullOrWhiteSpace(value) && value.Contains('@', StringComparison.Ordinal));
    }
}
