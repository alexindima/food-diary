using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid? UserId, Guid NotificationId) : ICommand<Result>, IUserRequest;
