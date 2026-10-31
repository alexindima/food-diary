using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Commands.ScheduleAdminTestNotification;

public sealed record ScheduleAdminTestNotificationCommand(
    Guid ActorUserId,
    Guid TargetUserId,
    int DelaySeconds,
    string Type) : ICommand<Result<ScheduledNotificationModel>>;
