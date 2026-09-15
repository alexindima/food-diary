using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotifications;

public record GetNotificationsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<NotificationModel>>>, IUserRequest;
