using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public class CreateCycleCommandValidator : AbstractValidator<CreateCycleCommand> {
    public CreateCycleCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(x => x.Mode)
            .Must(static mode => Enum.IsDefined((CycleTrackingMode)mode))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Mode is invalid.");

        RuleFor(x => x.AverageCycleLength)
            .InclusiveBetween(18, 60)
            .When(x => x.AverageCycleLength.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AverageCycleLength must be in range [18, 60].");

        RuleFor(x => x.AveragePeriodLength)
            .InclusiveBetween(1, 14)
            .When(x => x.AveragePeriodLength.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AveragePeriodLength must be in range [1, 14].");

        RuleFor(x => x.LutealLength)
            .InclusiveBetween(8, 18)
            .When(x => x.LutealLength.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("LutealLength must be in range [8, 18].");
    }
}
