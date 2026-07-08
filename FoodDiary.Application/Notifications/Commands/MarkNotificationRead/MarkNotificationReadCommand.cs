using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid? UserId, Guid NotificationId) : ICommand<Result>, IUserRequest;
