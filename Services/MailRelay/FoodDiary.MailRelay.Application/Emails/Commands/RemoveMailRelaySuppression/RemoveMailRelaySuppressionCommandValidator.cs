using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Commands.RemoveMailRelaySuppression;

public sealed class RemoveMailRelaySuppressionCommandValidator : AbstractValidator<RemoveMailRelaySuppressionCommand> {
    public RemoveMailRelaySuppressionCommandValidator() {
        RuleFor(static command => command.Email)
            .NotEmpty().WithErrorCode("Validation.Required")
            .EmailAddress().WithErrorCode("Validation.Invalid");
    }
}
