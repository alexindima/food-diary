namespace FoodDiary.MailRelay.Application.Options;

public sealed class MailRelayOptions {
    public const string SectionName = "MailRelay";

    public bool RequireApiKey { get; init; } = false;
    public string ApiKey { get; init; } = string.Empty;

    public static bool HasValidListenApiKey(MailRelayOptions options) {
        return !options.RequireApiKey || !string.IsNullOrWhiteSpace(options.ApiKey);
    }
}
