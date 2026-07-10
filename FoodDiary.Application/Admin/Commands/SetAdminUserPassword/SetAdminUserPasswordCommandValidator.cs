using FluentValidation;

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
            .MinimumLength(6)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("New password must be at least 6 characters.");
    }
}
