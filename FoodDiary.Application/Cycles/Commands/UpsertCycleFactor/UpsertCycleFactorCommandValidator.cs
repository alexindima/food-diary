using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;

public class UpsertCycleFactorCommandValidator : AbstractValidator<UpsertCycleFactorCommand> {
    public UpsertCycleFactorCommandValidator() {
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

        RuleFor(x => x.Type)
            .Must(static type => Enum.IsDefined((CycleFactorType)type))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Type is invalid.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("EndDate must be later than or equal to StartDate.");

        RuleFor(x => x)
            .Must(static x => !(x.ClearNotes && !string.IsNullOrWhiteSpace(x.Notes)))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Notes cannot be provided when ClearNotes is true.");
    }
}
