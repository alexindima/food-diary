using FluentValidation;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Admin.Commands.SetAdminUserPassword;

public sealed class SetAdminUserPasswordCommandValidator : AbstractValidator<SetAdminUserPasswordCommand> {
    public SetAdminUserPasswordCommandValidator() {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("User id is required.");

        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("New password is required.")
            .MinimumLength(AuthenticationInputLimits.MinimumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"New password must be at least {AuthenticationInputLimits.MinimumPasswordLength} characters.")
            .MaximumLength(AuthenticationInputLimits.MaximumPasswordLength)
            .WithErrorCode("Validation.Invalid")
            .WithMessage($"New password must not exceed {AuthenticationInputLimits.MaximumPasswordLength} characters.");
    }
}
