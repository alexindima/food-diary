using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;

public sealed class TelegramLoginWidgetCommandValidator : AbstractValidator<TelegramLoginWidgetCommand>
{
    public TelegramLoginWidgetCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.AuthDate).GreaterThan(0);
        RuleFor(x => x.Hash).NotEmpty();
    }
}
