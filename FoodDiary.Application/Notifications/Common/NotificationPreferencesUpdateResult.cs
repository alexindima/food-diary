using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public sealed record NotificationPreferencesUpdateResult(
    UserId UserId,
    NotificationPreferencesModel Preferences);
