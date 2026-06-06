using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Commands.ScheduleTestNotification;

public sealed record ScheduleTestNotificationCommand(
    Guid? UserId,
    int DelaySeconds,
    string Type) : ICommand<Result<ScheduledNotificationModel>>, IUserRequest;
