using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid? UserId, Guid NotificationId) : ICommand<Result>, IUserRequest;
