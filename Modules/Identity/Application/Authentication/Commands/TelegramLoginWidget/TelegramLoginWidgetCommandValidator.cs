using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.TelegramLoginWidget;

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
            .MaximumLength(AuthenticationInputLimits.MaximumTelegramHashLength)
            .WithErrorCode("Validation.Invalid");
        RuleFor(x => x.Username)
            .MaximumLength(AuthenticationInputLimits.MaximumTelegramUsernameLength);
        RuleFor(x => x.FirstName)
            .MaximumLength(AuthenticationInputLimits.MaximumTelegramNameLength);
        RuleFor(x => x.LastName)
            .MaximumLength(AuthenticationInputLimits.MaximumTelegramNameLength);
        RuleFor(x => x.PhotoUrl)
            .MaximumLength(AuthenticationInputLimits.MaximumTelegramPhotoUrlLength);
    }
}
