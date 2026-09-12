using FluentValidation;

namespace FoodDiary.Application.Identity.Authentication.Commands.ExchangeTelegramOidc;

public sealed class ExchangeTelegramOidcCommandValidator : AbstractValidator<ExchangeTelegramOidcCommand> {
    public ExchangeTelegramOidcCommandValidator() {
        RuleFor(command => command.Code).NotEmpty().WithErrorCode("Validation.Required").MaximumLength(4096).WithErrorCode("Validation.Invalid");
        RuleFor(command => command.State).NotEmpty().WithErrorCode("Validation.Required").Length(43).WithErrorCode("Validation.Invalid");
        RuleFor(command => command.BrowserBinding).NotEmpty().WithErrorCode("Validation.Required").Length(43).WithErrorCode("Validation.Invalid");
    }
}
