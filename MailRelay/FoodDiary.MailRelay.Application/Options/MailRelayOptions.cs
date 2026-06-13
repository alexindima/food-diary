namespace FoodDiary.MailRelay.Application.Options;

public sealed class MailRelayOptions {
    public const string SectionName = "MailRelay";

    public bool RequireApiKey { get; init; } = true;
    public string ApiKey { get; init; } = string.Empty;
    public bool RequireMailgunWebhookSignature { get; init; } = true;
    public string MailgunWebhookSigningKey { get; init; } = string.Empty;
    public bool RequireAwsSesSnsSignature { get; init; } = true;

    public static bool HasValidListenApiKey(MailRelayOptions options) {
        return options.RequireApiKey && !string.IsNullOrWhiteSpace(options.ApiKey);
    }

    public static bool HasValidProviderWebhookConfiguration(MailRelayOptions options) {
        return !options.RequireMailgunWebhookSignature ||
               !string.IsNullOrWhiteSpace(options.MailgunWebhookSigningKey);
    }
}
