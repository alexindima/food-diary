using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandValidator : AbstractValidator<TelegramVerifyCommand> {
    public TelegramVerifyCommandValidator() {
        RuleFor(x => x.InitData)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("initData is required.")
            .MaximumLength(AuthenticationInputLimits.MaximumTelegramInitDataLength)
            .WithErrorCode("Validation.Invalid");
    }
}
