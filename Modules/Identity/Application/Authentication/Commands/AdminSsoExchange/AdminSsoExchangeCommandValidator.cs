using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandValidator : AbstractValidator<AdminSsoExchangeCommand> {
    public AdminSsoExchangeCommandValidator() {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .MaximumLength(AuthenticationInputLimits.MaximumAdminSsoCodeLength)
            .WithErrorCode("Validation.Invalid");
    }
}
