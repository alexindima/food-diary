using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

public sealed record NotificationUserContext(UserId UserId, bool HasPassword, string? Language);
