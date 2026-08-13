using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand> {
    public RegisterCommandValidator() {
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
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Password must be at least 6 characters");
    }
}
