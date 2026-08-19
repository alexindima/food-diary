using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;

public sealed class SetAttentionSignalStateCommandValidator : AbstractValidator<SetAttentionSignalStateCommand> {
    public const int MaximumSignalIdLength = 200;

    public SetAttentionSignalStateCommandValidator() {
        RuleFor(command => command.ClientUserId).NotEmpty();
        RuleFor(command => command.SignalId).NotEmpty().MaximumLength(MaximumSignalIdLength);
        RuleFor(command => command.Action).Must(action =>
            string.Equals(action, "Acknowledge", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, "Snooze", StringComparison.OrdinalIgnoreCase));
        RuleFor(command => command.SnoozedUntilUtc)
            .NotNull()
            .When(command => string.Equals(command.Action, "Snooze", StringComparison.OrdinalIgnoreCase));
    }
}
