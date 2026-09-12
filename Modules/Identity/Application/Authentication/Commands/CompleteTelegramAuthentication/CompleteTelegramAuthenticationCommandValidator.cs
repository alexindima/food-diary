using FluentValidation;

namespace FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramAuthentication;

public sealed class CompleteTelegramAuthenticationCommandValidator : AbstractValidator<CompleteTelegramAuthenticationCommand> {
    public CompleteTelegramAuthenticationCommandValidator() {
        RuleFor(command => command.Ticket).NotEmpty().WithErrorCode("Validation.Required").Length(43).WithErrorCode("Validation.Invalid");
        RuleFor(command => command.BrowserBinding).NotEmpty().WithErrorCode("Validation.Required").Length(43).WithErrorCode("Validation.Invalid");
        RuleFor(command => command.Action).Must(action => action is "login" or "register" or "link").WithErrorCode("Validation.Invalid");
        RuleFor(command => command.Language).Must(language => language is null or "ru" or "en").WithErrorCode("Validation.Invalid");
        RuleFor(command => command.CurrentUserId).NotEmpty().WithErrorCode("Validation.Required")
            .When(command => string.Equals(command.Action, "link", StringComparison.Ordinal));
    }
}
