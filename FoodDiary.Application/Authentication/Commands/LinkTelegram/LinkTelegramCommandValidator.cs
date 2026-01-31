using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.LinkTelegram;

public sealed class LinkTelegramCommandValidator : AbstractValidator<LinkTelegramCommand>
{
    public LinkTelegramCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");

        RuleFor(x => x.InitData)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("initData is required.");
    }
}
