using FoodDiary.Modules.Identity.Contracts.Authentication;
using FluentValidation;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.GoogleLogin;

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand> {
    public GoogleLoginCommandValidator() {
        RuleFor(x => x.Credential)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("credential is required.")
            .MaximumLength(IdentityInputLimits.MaximumGoogleCredentialLength)
            .WithErrorCode("Validation.Invalid");
    }
}
