using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(Guid? UserId)
    : IQuery<Result<NotificationPreferencesModel>>, IUserRequest;
