using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed class TelegramBotAuthCommandValidator : AbstractValidator<TelegramBotAuthCommand>
{
    public TelegramBotAuthCommandValidator()
    {
        RuleFor(x => x.TelegramUserId).GreaterThan(0);
    }
}
