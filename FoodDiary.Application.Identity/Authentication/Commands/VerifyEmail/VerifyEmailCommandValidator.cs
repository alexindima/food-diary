using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Identity.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand> {
    public VerifyEmailCommandValidator() {
        RuleFor(x => x.UserId)
            .Must(userId => userId != Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("token is required.")
            .MaximumLength(AuthenticationInputLimits.MaximumOpaqueTokenLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"token must not exceed {AuthenticationInputLimits.MaximumOpaqueTokenLength} characters.");
    }
}
