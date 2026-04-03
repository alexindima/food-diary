using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand> {
    public GoogleLoginCommandValidator() {
        RuleFor(x => x.Credential)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("credential is required.");
    }
}
