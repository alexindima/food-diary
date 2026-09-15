using FluentValidation;

namespace FoodDiary.Modules.Notifications.Application.Commands.ScheduleTestNotification;

public sealed class ScheduleTestNotificationCommandValidator : AbstractValidator<ScheduleTestNotificationCommand> {
    public ScheduleTestNotificationCommandValidator() {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.DelaySeconds)
            .InclusiveBetween(1, 3600);
    }
}
