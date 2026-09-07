namespace FoodDiary.MailInbox.Application.Messages.Models;

public readonly record struct InboundMailAdmission(
    bool IsTrustedRelay,
    string? EnvelopeFromAddress = null) {
    public static InboundMailAdmission Untrusted { get; } = new(IsTrustedRelay: false);

    public static InboundMailAdmission TrustedRelay { get; } = new(IsTrustedRelay: true);
}
