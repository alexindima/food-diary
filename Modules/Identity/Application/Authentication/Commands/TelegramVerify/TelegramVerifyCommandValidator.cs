using FoodDiary.Modules.Identity.Contracts.Authentication;
using FluentValidation;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandValidator : AbstractValidator<TelegramVerifyCommand> {
    public TelegramVerifyCommandValidator() {
        RuleFor(x => x.InitData)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("initData is required.")
            .MaximumLength(IdentityInputLimits.MaximumTelegramInitDataLength)
            .WithErrorCode("Validation.Invalid");
    }
}
