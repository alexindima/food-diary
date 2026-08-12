using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public sealed record NotificationUserContext(UserId UserId, bool HasPassword, string? Language);
