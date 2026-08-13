using FluentValidation;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoStart;

public sealed class AdminSsoStartCommandValidator : AbstractValidator<AdminSsoStartCommand> {
    public AdminSsoStartCommandValidator() {
        RuleFor(x => x.UserId)
            .Must(userId => userId != Guid.Empty)
            .WithErrorCode("Validation.Required")
            .WithMessage("userId is required.");
    }
}
