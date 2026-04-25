using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class IngestManyMailRelayDeliveryEventsCommandValidator : AbstractValidator<IngestManyMailRelayDeliveryEventsCommand> {
    public IngestManyMailRelayDeliveryEventsCommandValidator() {
        RuleFor(static command => command.Requests)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleForEach(static command => command.Requests)
            .ChildRules(static request => {
                request.RuleFor(static value => value.EventType)
                    .NotEmpty().WithErrorCode("Validation.Required")
                    .Must(MailRelayDeliveryEventType.IsSupported)
                    .WithErrorCode("Validation.Invalid")
                    .WithMessage("EventType must be either 'bounce' or 'complaint'.");
                request.RuleFor(static value => value.Email)
                    .NotEmpty().WithErrorCode("Validation.Required")
                    .EmailAddress().WithErrorCode("Validation.Invalid");
                request.RuleFor(static value => value.Source)
                    .NotEmpty().WithErrorCode("Validation.Required");
                request.RuleFor(static value => value.Classification)
                    .Must(MailRelayBounceClassification.IsSupportedOptional)
                    .WithErrorCode("Validation.Invalid")
                    .WithMessage("Classification must be either 'hard' or 'soft' when provided.");
            });
    }
}
