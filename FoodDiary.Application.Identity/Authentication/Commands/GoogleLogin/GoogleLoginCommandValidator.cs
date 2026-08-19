using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand> {
    public GoogleLoginCommandValidator() {
        RuleFor(x => x.Credential)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("credential is required.")
            .MaximumLength(AuthenticationInputLimits.MaximumGoogleCredentialLength)
            .WithErrorCode("Validation.Invalid");
    }
}
