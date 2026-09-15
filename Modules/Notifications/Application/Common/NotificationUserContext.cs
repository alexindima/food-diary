using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

public sealed record NotificationUserContext(UserId UserId, bool HasPassword, string? Language);
