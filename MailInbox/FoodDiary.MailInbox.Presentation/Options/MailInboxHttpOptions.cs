namespace FoodDiary.MailInbox.Presentation.Options;

public sealed class MailInboxHttpOptions {
    public const string SectionName = "MailInboxHttp";
    public const int MinApiKeyLength = 32;
    public const int MaxApiKeyLength = 256;

    public bool RequireApiKey { get; init; } = true;
    public string ApiKey { get; init; } = string.Empty;

    public int MaxConcurrentMessageDetailRequests { get; init; } = 2;

    public TimeSpan MessageDetailQueueTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public int MaxConcurrentReadinessRequests { get; init; } = 1;

    public TimeSpan ReadinessQueueTimeout { get; init; } = TimeSpan.FromMilliseconds(250);

    public TimeSpan ReadinessExecutionTimeout { get; init; } = TimeSpan.FromSeconds(5);

    public static bool HasValidApiKey(MailInboxHttpOptions options) {
        return options.RequireApiKey &&
               !string.IsNullOrWhiteSpace(options.ApiKey) &&
               options.ApiKey.Length is >= MinApiKeyLength and <= MaxApiKeyLength &&
               options.MaxConcurrentMessageDetailRequests is > 0 and <= 64 &&
               options.MessageDetailQueueTimeout > TimeSpan.Zero &&
               options.MessageDetailQueueTimeout <= TimeSpan.FromSeconds(30) &&
               options.MaxConcurrentReadinessRequests is > 0 and <= 4 &&
               options.ReadinessQueueTimeout > TimeSpan.Zero &&
               options.ReadinessQueueTimeout <= TimeSpan.FromSeconds(5) &&
               options.ReadinessExecutionTimeout > TimeSpan.Zero &&
               options.ReadinessExecutionTimeout <= TimeSpan.FromSeconds(30);
    }
}
