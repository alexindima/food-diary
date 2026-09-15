using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Commands.ScheduleTestNotification;

public sealed record ScheduleTestNotificationCommand(
    Guid? UserId,
    int DelaySeconds,
    string Type) : ICommand<Result<ScheduledNotificationModel>>, IUserRequest;
