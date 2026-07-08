using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(Guid? UserId) : IQuery<Result<IReadOnlyList<NotificationModel>>>, IUserRequest;
