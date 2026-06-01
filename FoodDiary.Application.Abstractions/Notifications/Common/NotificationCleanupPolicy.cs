namespace FoodDiary.Application.Abstractions.Notifications.Common;

public sealed record NotificationCleanupPolicy(
    IReadOnlyCollection<string> TransientTypes,
    int TransientReadRetentionDays,
    int TransientUnreadRetentionDays,
    int StandardReadRetentionDays,
    int StandardUnreadRetentionDays,
    int BatchSize);
