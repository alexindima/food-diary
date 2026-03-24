using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationCommandValidator : AbstractValidator<ResendEmailVerificationCommand> {
    public ResendEmailVerificationCommandValidator() {
        RuleFor(x => x.UserId)
            .Must(userId => userId != Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");
    }
}
