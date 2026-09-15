using FoodDiary.Modules.Identity.Contracts.Authentication;
using FluentValidation;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoExchange;

public sealed class AdminSsoExchangeCommandValidator : AbstractValidator<AdminSsoExchangeCommand> {
    public AdminSsoExchangeCommandValidator() {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .MaximumLength(IdentityInputLimits.MaximumAdminSsoCodeLength)
            .WithErrorCode("Validation.Invalid");
    }
}
