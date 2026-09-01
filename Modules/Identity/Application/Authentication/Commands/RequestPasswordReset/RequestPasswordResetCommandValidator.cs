using FluentValidation;

namespace FoodDiary.Application.Identity.Authentication.Commands.RequestPasswordReset;

public sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand> {
    public RequestPasswordResetCommandValidator() {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Email is required")
            .EmailAddress()
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Invalid email address");
    }
}
