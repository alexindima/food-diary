using FluentValidation;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;

namespace FoodDiary.Modules.Wearables.Application.Commands.ConnectWearable;

public sealed class ConnectWearableCommandValidator : AbstractValidator<ConnectWearableCommand> {
    public ConnectWearableCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumProviderLength);
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumAuthorizationCodeLength);
        RuleFor(command => command.State)
            .NotEmpty()
            .MaximumLength(WearableInputLimits.MaximumProtectedOAuthStateLength);
        RuleFor(command => command.RequestId)
            .NotEmpty()
            .Length(64)
            .Matches("^[0-9A-F]{64}$");
        RuleFor(command => command.RequestHash)
            .NotEmpty()
            .Length(64)
            .Matches("^[0-9A-F]{64}$");
    }
}
