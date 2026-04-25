using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestMailRelayDeliveryEventCommandValidator : AbstractValidator<IngestMailRelayDeliveryEventCommand> {
    public IngestMailRelayDeliveryEventCommandValidator() {
        RuleFor(static command => command.Request.EventType)
            .NotEmpty().WithErrorCode("Validation.Required")
            .Must(MailRelayDeliveryEventType.IsSupported)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("EventType must be either 'bounce' or 'complaint'.");
        RuleFor(static command => command.Request.Email)
            .NotEmpty().WithErrorCode("Validation.Required")
            .EmailAddress().WithErrorCode("Validation.Invalid");
        RuleFor(static command => command.Request.Source)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(static command => command.Request.Classification)
            .Must(MailRelayBounceClassification.IsSupportedOptional)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Classification must be either 'hard' or 'soft' when provided.");
    }
}
