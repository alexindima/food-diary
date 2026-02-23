using FluentValidation;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandValidator : AbstractValidator<AdminSsoStartCommand> {
    public AdminSsoStartCommandValidator() {
        RuleFor(x => x.UserId)
            .Must(userId => userId != UserId.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");
    }
}
