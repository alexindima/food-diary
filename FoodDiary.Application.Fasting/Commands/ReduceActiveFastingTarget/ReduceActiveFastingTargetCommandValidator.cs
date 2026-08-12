using FluentValidation;

namespace FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;

public sealed class ReduceActiveFastingTargetCommandValidator : AbstractValidator<ReduceActiveFastingTargetCommand> {
    public ReduceActiveFastingTargetCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Authentication.InvalidToken")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Authentication.InvalidToken");

        RuleFor(x => x.ReducedHours)
            .GreaterThan(0)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Reduced fasting hours must be greater than zero");
    }
}
