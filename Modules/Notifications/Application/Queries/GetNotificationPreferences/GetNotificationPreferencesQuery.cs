using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(Guid? UserId)
    : IQuery<Result<NotificationPreferencesModel>>, IUserRequest;
