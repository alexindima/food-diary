using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public sealed class ConfirmPasswordResetCommandValidator : AbstractValidator<ConfirmPasswordResetCommand> {
    public ConfirmPasswordResetCommandValidator() {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Password must be at least 6 characters");
    }
}
