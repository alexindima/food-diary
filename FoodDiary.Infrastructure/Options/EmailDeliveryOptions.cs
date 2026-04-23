namespace FoodDiary.Infrastructure.Options;

public sealed class EmailDeliveryOptions {
    public const string SectionName = "EmailDelivery";
    public const string SmtpMode = "Smtp";
    public const string RelayMode = "Relay";

    public string Mode { get; init; } = SmtpMode;
    public string RelayBaseUrl { get; init; } = string.Empty;
    public string RelayApiKey { get; init; } = string.Empty;

    public static bool HasSupportedMode(EmailDeliveryOptions options) {
        return options.Mode is SmtpMode or RelayMode;
    }

    public static bool HasValidRelayBaseUrl(EmailDeliveryOptions options) {
        if (!string.Equals(options.Mode, RelayMode, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return Uri.IsWellFormedUriString(options.RelayBaseUrl, UriKind.Absolute);
    }
}
