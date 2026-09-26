using FluentValidation;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand> {
    public RefreshTokenCommandValidator() {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("RefreshToken is required")
            .MaximumLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)
            .WithErrorCode("Validation.Invalid");
    }
}
