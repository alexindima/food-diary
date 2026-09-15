using FoodDiary.Modules.Identity.Contracts.Authentication;
using FluentValidation;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandValidator : AbstractValidator<TelegramLoginWidgetCommand> {
    public TelegramLoginWidgetCommandValidator() {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("id must be greater than 0.");

        RuleFor(x => x.AuthDate)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("authDate must be greater than 0.");

        RuleFor(x => x.Hash)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("hash is required.")
            .MaximumLength(IdentityInputLimits.MaximumTelegramHashLength)
            .WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Username)
            .MaximumLength(IdentityInputLimits.MaximumTelegramUsernameLength);
        RuleFor(x => x.FirstName)
            .MaximumLength(IdentityInputLimits.MaximumTelegramNameLength);
        RuleFor(x => x.LastName)
            .MaximumLength(IdentityInputLimits.MaximumTelegramNameLength);
        RuleFor(x => x.PhotoUrl)
            .MaximumLength(IdentityInputLimits.MaximumTelegramPhotoUrlLength);
    }
}
