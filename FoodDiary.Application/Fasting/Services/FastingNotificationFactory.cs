using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingNotificationFactory {
    public static Notification Create(FastingNotificationCandidate candidate) {
        return candidate.Type switch {
            NotificationTypes.FastingCompleted => NotificationFactory.CreateFastingCompleted(
                candidate.UserId,
                candidate.PlanType ?? string.Empty,
                candidate.OccurrenceKind ?? string.Empty,
                candidate.ReferenceId),
            NotificationTypes.FastingCheckInReminder => NotificationFactory.CreateFastingCheckInReminder(
                candidate.UserId,
                candidate.ReferenceId),
            NotificationTypes.EatingWindowStarted => NotificationFactory.CreateEatingWindowStarted(
                candidate.UserId,
                candidate.PlanType ?? string.Empty,
                candidate.OccurrenceKind ?? string.Empty,
                candidate.ReferenceId),
            NotificationTypes.FastingWindowStarted => NotificationFactory.CreateFastingWindowStarted(
                candidate.UserId,
                candidate.PlanType ?? string.Empty,
                candidate.OccurrenceKind ?? string.Empty,
                candidate.ReferenceId),
            _ => throw new InvalidOperationException($"Unsupported fasting notification type '{candidate.Type}'."),
        };
    }
}
