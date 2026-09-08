using FluentValidation;

namespace FoodDiary.MailRelay.Application.Emails.Commands.EnqueueMailRelayEmail;

public sealed class EnqueueMailRelayEmailCommandValidator : AbstractValidator<EnqueueMailRelayEmailCommand> {
    public EnqueueMailRelayEmailCommandValidator() {
        RuleFor(x => x.Request.Purpose).NotEmpty().MaximumLength(64).Matches("^[a-z0-9_]+$");
        RuleFor(x => x.Request.ReplyTo).EmailAddress().MaximumLength(320).Must(x => x is null || (!x.Contains('\r', StringComparison.Ordinal) && !x.Contains('\n', StringComparison.Ordinal)));
        RuleFor(x => x.Request.InReplyTo).MaximumLength(998).Must(x => x is null || (!x.Contains('\r', StringComparison.Ordinal) && !x.Contains('\n', StringComparison.Ordinal)));
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
