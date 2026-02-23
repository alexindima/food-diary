using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationCommandValidator : AbstractValidator<ResendEmailVerificationCommand> {
    public ResendEmailVerificationCommandValidator() {
        RuleFor(x => x.UserId)
            .Must(userId => userId != UserId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");
    }
}
