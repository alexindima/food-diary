namespace FoodDiary.MailInbox.Presentation.Options;

public sealed class MailInboxHttpOptions {
    public const string SectionName = "MailInboxHttp";
    public const int MinApiKeyLength = 32;
    public const int MaxApiKeyLength = 256;
    private static readonly HashSet<string> KnownInsecureApiKeys = new(StringComparer.Ordinal) {
        "0123456789abcdef0123456789abcdea",
        "0123456789abcdef0123456789abcdeb",
        "0123456789abcdef0123456789abcdec",
    };

    public string MetadataApiKey { get; init; } = string.Empty;
    public string ContentApiKey { get; init; } = string.Empty;
    public string StateApiKey { get; init; } = string.Empty;

    public int MaxConcurrentMessageDetailRequests { get; init; } = 2;

    public TimeSpan MessageDetailQueueTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public TimeSpan MessageDetailExecutionTimeout { get; init; } = TimeSpan.FromSeconds(15);

    public int MaxConcurrentMessageMetadataRequests { get; init; } = 4;

    public TimeSpan MessageMetadataQueueTimeout { get; init; } = TimeSpan.FromMilliseconds(250);

    public TimeSpan MessageMetadataExecutionTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public int MaxConcurrentReadinessRequests { get; init; } = 1;

    public TimeSpan ReadinessQueueTimeout { get; init; } = TimeSpan.FromMilliseconds(250);

    public TimeSpan ReadinessExecutionTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public static bool HasValidConfiguration(MailInboxHttpOptions options) {
        return HasValidKey(options.MetadataApiKey) &&
               HasValidKey(options.ContentApiKey) &&
               HasValidKey(options.StateApiKey) &&
               !string.Equals(options.MetadataApiKey, options.ContentApiKey, StringComparison.Ordinal) &&
               !string.Equals(options.MetadataApiKey, options.StateApiKey, StringComparison.Ordinal) &&
               !string.Equals(options.ContentApiKey, options.StateApiKey, StringComparison.Ordinal) &&
               options.MaxConcurrentMessageDetailRequests is > 0 and <= 64 &&
               options.MessageDetailQueueTimeout > TimeSpan.Zero &&
               options.MessageDetailQueueTimeout <= TimeSpan.FromSeconds(30) &&
               options.MessageDetailExecutionTimeout > TimeSpan.Zero &&
               options.MessageDetailExecutionTimeout <= TimeSpan.FromSeconds(30) &&
               options.MaxConcurrentMessageMetadataRequests is > 0 and <= 64 &&
               options.MessageMetadataQueueTimeout > TimeSpan.Zero &&
               options.MessageMetadataQueueTimeout <= TimeSpan.FromSeconds(5) &&
               options.MessageMetadataExecutionTimeout > TimeSpan.Zero &&
               options.MessageMetadataExecutionTimeout <= TimeSpan.FromSeconds(30) &&
               options.MaxConcurrentReadinessRequests is > 0 and <= 4 &&
               options.ReadinessQueueTimeout > TimeSpan.Zero &&
               options.ReadinessQueueTimeout <= TimeSpan.FromSeconds(5) &&
               options.ReadinessExecutionTimeout > TimeSpan.Zero &&
               options.ReadinessExecutionTimeout <= TimeSpan.FromSeconds(30);
    }

    public string GetApiKey(Security.MailInboxPermission permission) => permission switch {
        Security.MailInboxPermission.Metadata => MetadataApiKey,
        Security.MailInboxPermission.Content => ContentApiKey,
        Security.MailInboxPermission.State => StateApiKey,
        _ => string.Empty,
    };

    private static bool HasValidKey(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length is >= MinApiKeyLength and <= MaxApiKeyLength &&
        !KnownInsecureApiKeys.Contains(value);
}
