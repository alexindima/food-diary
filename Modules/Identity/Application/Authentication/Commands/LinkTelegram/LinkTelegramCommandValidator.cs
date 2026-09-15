using FoodDiary.Modules.Identity.Contracts.Authentication;
using FluentValidation;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandValidator : AbstractValidator<LinkTelegramCommand> {
    public LinkTelegramCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");

        RuleFor(x => x.InitData)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("initData is required.")
            .MaximumLength(IdentityInputLimits.MaximumTelegramInitDataLength)
            .WithErrorCode("Validation.Invalid");
    }
}
