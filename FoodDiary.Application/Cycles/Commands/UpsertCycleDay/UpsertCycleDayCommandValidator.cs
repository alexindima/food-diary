using FluentValidation;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public class UpsertCycleDayCommandValidator : AbstractValidator<UpsertCycleDayCommand> {
    public UpsertCycleDayCommandValidator() {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(x => x.CycleId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CycleId is invalid.");

        RuleFor(x => x.Symptoms)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Symptoms are required.");

        RuleFor(x => x.Symptoms)
            .SetValidator(new DailySymptomsModelValidator());
    }

    private sealed class DailySymptomsModelValidator : AbstractValidator<DailySymptomsModel> {
        public DailySymptomsModelValidator() {
            RuleFor(x => x.Pain).InclusiveBetween(0, 9);
            RuleFor(x => x.Mood).InclusiveBetween(0, 9);
            RuleFor(x => x.Edema).InclusiveBetween(0, 9);
            RuleFor(x => x.Headache).InclusiveBetween(0, 9);
            RuleFor(x => x.Energy).InclusiveBetween(0, 9);
            RuleFor(x => x.SleepQuality).InclusiveBetween(0, 9);
            RuleFor(x => x.Libido).InclusiveBetween(0, 9);
        }
    }
}
