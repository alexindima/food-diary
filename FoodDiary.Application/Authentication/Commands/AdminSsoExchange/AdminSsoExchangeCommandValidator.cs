using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandValidator : AbstractValidator<AdminSsoExchangeCommand>
{
    public AdminSsoExchangeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithErrorCode("Validation.Required");
    }
}
