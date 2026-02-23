using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand> {
    public VerifyEmailCommandValidator() {
        RuleFor(x => x.UserId)
            .Must(userId => userId != UserId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("token is required.");
    }
}
