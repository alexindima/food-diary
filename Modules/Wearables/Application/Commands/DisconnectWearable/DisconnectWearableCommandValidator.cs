using FluentValidation;
using FoodDiary.Application.Abstractions.Wearables.Common;

namespace FoodDiary.Application.Wearables.Commands.DisconnectWearable;

public sealed class DisconnectWearableCommandValidator : AbstractValidator<DisconnectWearableCommand> {
    public DisconnectWearableCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumProviderLength);
    }
}
