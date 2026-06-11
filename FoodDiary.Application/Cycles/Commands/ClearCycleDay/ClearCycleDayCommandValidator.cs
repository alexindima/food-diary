using FluentValidation;

namespace FoodDiary.Application.Cycles.Commands.ClearCycleDay;

public class ClearCycleDayCommandValidator : AbstractValidator<ClearCycleDayCommand> {
    public ClearCycleDayCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(x => x.CycleProfileId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CycleProfileId is invalid.");
    }
}
