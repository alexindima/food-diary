using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class CreateMailRelaySuppressionCommandValidator : AbstractValidator<CreateMailRelaySuppressionCommand> {
    public CreateMailRelaySuppressionCommandValidator() {
        RuleFor(static command => command.Request.Email)
            .NotEmpty().WithErrorCode("Validation.Required")
            .EmailAddress().WithErrorCode("Validation.Invalid");
        RuleFor(static command => command.Request.Reason)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(static command => command.Request.Source)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(static command => command.Request.ExpiresAtUtc)
            .Must(static value => value is null || value > DateTimeOffset.UtcNow)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("ExpiresAtUtc must be in the future when provided.");
    }
}
