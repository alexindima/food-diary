using FluentValidation;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Wearables.Wearables.Common;

namespace FoodDiary.Application.Wearables.Wearables.Commands.SyncWearableData;

public sealed class SyncWearableDataCommandValidator : AbstractValidator<SyncWearableDataCommand> {
    public SyncWearableDataCommandValidator(TimeProvider timeProvider) {
        RuleFor(command => command.UserId)
            .NotEmpty();
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumProviderLength);
        RuleFor(command => command.Date)
            .Must(date => WearableDateRules.IsSupported(date, timeProvider))
            .WithErrorCode("Validation.Invalid")
            .WithMessage("Date must be between 1970-01-01 and the current UTC date.");
    }
}
