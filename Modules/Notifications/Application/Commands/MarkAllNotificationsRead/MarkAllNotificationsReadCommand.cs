using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Commands.MarkAllNotificationsRead;

public record MarkAllNotificationsReadCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
