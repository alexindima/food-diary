using FluentValidation;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand> {
    public ChangePasswordCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId is not null && userId.Value != UserId.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("New password is required")
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("New password must be at least 6 characters");

        RuleFor(x => x)
            .Must(command => command.CurrentPassword != command.NewPassword)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("New password must differ from the current password");
    }
}