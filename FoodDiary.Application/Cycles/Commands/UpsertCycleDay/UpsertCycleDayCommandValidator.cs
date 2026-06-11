using FluentValidation;
using FoodDiary.Domain.Enums;
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

        RuleFor(x => x.CycleProfileId)
            .NotEqual(Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("CycleProfileId is invalid.");

        RuleFor(x => x.Symptoms)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("Symptoms are required.");

        RuleForEach(x => x.Symptoms)
            .SetValidator(new SymptomLogCommandModelValidator());

        RuleFor(x => x.Bleeding)
            .SetValidator(new BleedingLogCommandModelValidator()!)
            .When(x => x.Bleeding is not null);

        RuleFor(x => x.FertilitySignal)
            .SetValidator(new FertilitySignalCommandModelValidator()!)
            .When(x => x.FertilitySignal is not null);
    }

    private sealed class BleedingLogCommandModelValidator : AbstractValidator<BleedingLogCommandModel> {
        public BleedingLogCommandModelValidator() {
            RuleFor(x => x.Type).Must(static type => Enum.IsDefined((BleedingType)type));
            RuleFor(x => x.Flow).Must(static flow => Enum.IsDefined((CycleFlowLevel)flow));
            RuleFor(x => x.PainImpact).InclusiveBetween(0, 10).When(x => x.PainImpact.HasValue);
            RuleFor(x => x)
                .Must(x => !(x.ClearNotes && !string.IsNullOrWhiteSpace(x.Notes)))
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Notes cannot be provided when ClearNotes is true.");
        }
    }

    private sealed class SymptomLogCommandModelValidator : AbstractValidator<SymptomLogCommandModel> {
        public SymptomLogCommandModelValidator() {
            RuleFor(x => x.Category).Must(static category => Enum.IsDefined((CycleSymptomCategory)category));
            RuleFor(x => x.Intensity).InclusiveBetween(0, 10);
            RuleFor(x => x.Tags).NotNull();
            RuleFor(x => x)
                .Must(x => !(x.ClearNote && !string.IsNullOrWhiteSpace(x.Note)))
                .WithErrorCode("Validation.Invalid")
                .WithMessage("Note cannot be provided when ClearNote is true.");
        }
    }

    private sealed class FertilitySignalCommandModelValidator : AbstractValidator<FertilitySignalCommandModel> {
        public FertilitySignalCommandModelValidator() {
            RuleFor(x => x.BasalBodyTemperatureCelsius).InclusiveBetween(34, 42).When(x => x.BasalBodyTemperatureCelsius.HasValue);
            RuleFor(x => x.OvulationTestResult)
                .Must(static result => result.HasValue && Enum.IsDefined((OvulationTestResult)result.Value))
                .When(x => x.OvulationTestResult.HasValue);
            RuleFor(x => x)
                .Must(x => !(x.ClearNotes && !string.IsNullOrWhiteSpace(x.Notes)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Notes cannot be provided when ClearNotes is true.");
        }
    }
}
