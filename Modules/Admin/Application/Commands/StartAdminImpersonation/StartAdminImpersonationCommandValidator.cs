using FluentValidation;

namespace FoodDiary.Application.Admin.Commands.StartAdminImpersonation;

public sealed class StartAdminImpersonationCommandValidator : AbstractValidator<StartAdminImpersonationCommand> {
    public StartAdminImpersonationCommandValidator() {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(x => x.TargetUserId).NotEmpty().WithErrorCode("Validation.Required");
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(500)
            .WithErrorCode("Validation.Invalid");
    }
}
