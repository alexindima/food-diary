using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.RestoreAccount;

public sealed class RestoreAccountCommandValidator : AbstractValidator<RestoreAccountCommand> {
    public RestoreAccountCommandValidator() {
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
            .MinimumLength(AuthenticationInputLimits.MinimumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Password must be at least {AuthenticationInputLimits.MinimumPasswordLength} characters")
            .MaximumLength(AuthenticationInputLimits.MaximumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"Password must not exceed {AuthenticationInputLimits.MaximumPasswordLength} characters");
    }
}
