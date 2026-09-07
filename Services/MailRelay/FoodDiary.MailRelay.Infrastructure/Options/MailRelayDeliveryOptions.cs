namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class MailRelayDeliveryOptions {
    public const string SectionName = "MailRelayDelivery";
    public const string SmtpSubmissionMode = "SmtpSubmission";
    public const string DirectMxMode = "DirectMx";

    public string Mode { get; init; } = SmtpSubmissionMode;

    public static bool HasSupportedMode(MailRelayDeliveryOptions options) {
        return options.Mode is SmtpSubmissionMode or DirectMxMode;
    }
}
