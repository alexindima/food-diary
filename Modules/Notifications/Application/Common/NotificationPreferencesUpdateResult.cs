using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

public sealed record NotificationPreferencesUpdateResult(
    UserId UserId,
    NotificationPreferencesModel Preferences);
