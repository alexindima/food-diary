using FluentValidation;

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

        RuleFor(x => x.AverageLength)
            .InclusiveBetween(18, 60)
            .When(x => x.AverageLength.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AverageLength must be in range [18, 60].");

        RuleFor(x => x.LutealLength)
            .InclusiveBetween(8, 18)
            .When(x => x.LutealLength.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("LutealLength must be in range [8, 18].");
    }
}
