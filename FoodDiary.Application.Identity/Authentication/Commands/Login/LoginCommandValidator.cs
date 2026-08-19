using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand> {
    public LoginCommandValidator() {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Email is required")
            .EmailAddress()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Password is required")
            .MaximumLength(AuthenticationInputLimits.MaximumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Password must not exceed {AuthenticationInputLimits.MaximumPasswordLength} characters");
    }
}
