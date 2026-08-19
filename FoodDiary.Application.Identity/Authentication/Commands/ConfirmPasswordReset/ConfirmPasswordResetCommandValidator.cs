using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandValidator : AbstractValidator<ConfirmPasswordResetCommand> {
    public ConfirmPasswordResetCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Token is required")
            .MaximumLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Token must not exceed {AuthenticationInputLimits.MaximumOpaqueTokenLength} characters");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Password is required")
            .MinimumLength(AuthenticationInputLimits.MinimumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Password must be at least {AuthenticationInputLimits.MinimumPasswordLength} characters")
            .MaximumLength(AuthenticationInputLimits.MaximumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Password must not exceed {AuthenticationInputLimits.MaximumPasswordLength} characters");
    }
}
