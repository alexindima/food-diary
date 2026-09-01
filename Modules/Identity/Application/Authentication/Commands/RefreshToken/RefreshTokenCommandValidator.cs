using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.RefreshToken;

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
