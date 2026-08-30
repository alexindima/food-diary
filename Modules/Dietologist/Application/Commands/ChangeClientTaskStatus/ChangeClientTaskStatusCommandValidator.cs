using FluentValidation;

namespace FoodDiary.Application.Dietologist.Commands.ChangeClientTaskStatus;

public sealed class ChangeClientTaskStatusCommandValidator : AbstractValidator<ChangeClientTaskStatusCommand> {
    public ChangeClientTaskStatusCommandValidator() {
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.Status)
            .Must(status =>
                string.Equals(status, "Open", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase));
    }
}
