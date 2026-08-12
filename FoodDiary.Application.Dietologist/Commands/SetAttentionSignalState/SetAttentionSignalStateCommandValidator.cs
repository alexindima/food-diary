using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.SetAttentionSignalState;

internal sealed class SetAttentionSignalStateCommandValidator : AbstractValidator<SetAttentionSignalStateCommand> {
    public SetAttentionSignalStateCommandValidator() {
        RuleFor(command => command.ClientUserId).NotEmpty();
        RuleFor(command => command.SignalId).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Action).Must(action =>
            string.Equals(action, "Acknowledge", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(action, "Snooze", StringComparison.OrdinalIgnoreCase));
        RuleFor(command => command.SnoozedUntilUtc)
            .NotNull()
            .When(command => string.Equals(command.Action, "Snooze", StringComparison.OrdinalIgnoreCase));
    }
}
