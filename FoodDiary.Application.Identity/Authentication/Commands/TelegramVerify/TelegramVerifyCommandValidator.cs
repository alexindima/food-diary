using FluentValidation;

namespace FoodDiary.Application.Identity.Authentication.Commands.TelegramVerify;

public sealed class TelegramVerifyCommandValidator : AbstractValidator<TelegramVerifyCommand> {
    public TelegramVerifyCommandValidator() {
        RuleFor(x => x.InitData)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("initData is required.");
    }
}
