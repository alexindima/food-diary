using FluentValidation;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;

namespace FoodDiary.Modules.Wearables.Application.Commands.DisconnectWearable;

public sealed class DisconnectWearableCommandValidator : AbstractValidator<DisconnectWearableCommand> {
    public DisconnectWearableCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumProviderLength);
    }
}
