using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Commands;

public sealed class EnqueueMailRelayEmailCommandValidator : AbstractValidator<EnqueueMailRelayEmailCommand> {
    public EnqueueMailRelayEmailCommandValidator() {
        RuleFor(static command => command.Request.FromAddress)
            .NotEmpty().WithErrorCode("Validation.Required")
            .EmailAddress().WithErrorCode("Validation.Invalid");
        RuleFor(static command => command.Request.FromName)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(static command => command.Request.To)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleForEach(static command => command.Request.To)
            .NotEmpty().WithErrorCode("Validation.Required")
            .EmailAddress().WithErrorCode("Validation.Invalid");
        RuleFor(static command => command.Request.Subject)
            .NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(static command => command.Request.HtmlBody)
            .NotEmpty().WithErrorCode("Validation.Required");
    }
}
