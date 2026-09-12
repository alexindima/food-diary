using FluentValidation;

namespace FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;

public sealed class BeginTelegramMiniAppCommandValidator : AbstractValidator<BeginTelegramMiniAppCommand> {
    public BeginTelegramMiniAppCommandValidator() {
        RuleFor(command => command.InitData).NotEmpty().WithErrorCode("Validation.Required").MaximumLength(16384).WithErrorCode("Validation.Invalid");
        RuleFor(command => command.BrowserBinding).NotEmpty().WithErrorCode("Validation.Required").Length(43).WithErrorCode("Validation.Invalid");
        RuleFor(command => command.LinkUserId).NotEqual(Guid.Empty).WithErrorCode("Validation.Invalid").When(command => command.LinkUserId.HasValue);
    }
}
