using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

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
            .MinimumLength(AuthenticationInputLimits.MinimumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"New password must be at least {AuthenticationInputLimits.MinimumPasswordLength} characters")
            .MaximumLength(AuthenticationInputLimits.MaximumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"New password must not exceed {AuthenticationInputLimits.MaximumPasswordLength} characters");
    }
}
