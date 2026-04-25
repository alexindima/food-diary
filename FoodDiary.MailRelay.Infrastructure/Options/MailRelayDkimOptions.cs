namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class MailRelayDkimOptions {
    public const string SectionName = "MailRelayDkim";

    public bool Enabled { get; init; }

    public string? Domain { get; init; }

    public string? Selector { get; init; }

    public string? PrivateKeyPem { get; init; }

    public string? PrivateKeyPath { get; init; }

    public static bool HasValidConfiguration(MailRelayDkimOptions options) {
        if (!options.Enabled) {
            return true;
        }

        if (string.IsNullOrWhiteSpace(options.Domain) || string.IsNullOrWhiteSpace(options.Selector)) {
            return false;
        }

        var hasInlineKey = !string.IsNullOrWhiteSpace(options.PrivateKeyPem);
        var hasKeyPath = !string.IsNullOrWhiteSpace(options.PrivateKeyPath);
        return hasInlineKey ^ hasKeyPath;
    }
}
