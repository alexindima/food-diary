using FluentValidation;
using FoodDiary.Modules.Notifications.Contracts.Common;

namespace FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;

public sealed class ScheduleAdminTestNotificationCommandValidator : AbstractValidator<ScheduleAdminTestNotificationCommand> {
    public ScheduleAdminTestNotificationCommandValidator() {
        RuleFor(command => command.ActorUserId).NotEmpty();
        RuleFor(command => command.TargetUserId).NotEmpty();
        RuleFor(command => command.DelaySeconds).InclusiveBetween(1, 3600);
        RuleFor(command => command.Type).Must(type => type is
            NotificationTypes.FastingCompleted or
            NotificationTypes.FastingCheckInReminder or
            NotificationTypes.EatingWindowStarted or
            NotificationTypes.FastingWindowStarted);
    }
}
