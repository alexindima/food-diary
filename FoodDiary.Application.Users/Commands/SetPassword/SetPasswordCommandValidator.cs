using FluentValidation;

namespace FoodDiary.Application.Users.Commands.SetPassword;

public sealed class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand> {
    public SetPasswordCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user")
            .Must(userId => userId.HasValue && userId.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken")
            .WithMessage("Unable to identify user");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("New password is required")
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("New password must be at least 6 characters");
    }
}
