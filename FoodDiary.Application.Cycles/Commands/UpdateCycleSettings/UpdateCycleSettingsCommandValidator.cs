using FluentValidation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Cycles.Commands.UpdateCycleSettings;

public sealed class UpdateCycleSettingsCommandValidator : AbstractValidator<UpdateCycleSettingsCommand> {
    public UpdateCycleSettingsCommandValidator() {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithErrorCode("Validation.Required")
            .WithMessage("UserId is required.")
            .Must(id => id is not null && id.Value != Guid.Empty)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("UserId is invalid.");

        RuleFor(x => x.CycleProfileId)
            .NotEmpty()
            .WithErrorCode("Validation.Required")
            .WithMessage("CycleProfileId is required.");

        RuleFor(x => x.Mode)
            .Must(static mode => Enum.IsDefined((CycleTrackingMode)mode))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Mode is invalid.");

        RuleFor(x => x.AverageCycleLength)
            .InclusiveBetween(18, 60)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AverageCycleLength must be in range [18, 60].");

        RuleFor(x => x.AveragePeriodLength)
            .InclusiveBetween(1, 14)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("AveragePeriodLength must be in range [1, 14].");

        RuleFor(x => x.LutealLength)
            .InclusiveBetween(8, 18)
            .WithErrorCode("Validation.Invalid")
            .WithMessage("LutealLength must be in range [8, 18].");
    }
}
