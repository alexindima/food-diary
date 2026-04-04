using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
