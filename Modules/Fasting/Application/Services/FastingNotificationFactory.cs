using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Modules.Fasting.Application.Services;

internal static class FastingNotificationFactory {
    public static Notification Create(FastingNotificationCandidate candidate) {
        return candidate.Type switch {
            NotificationTypes.FastingCompleted => CreatePhaseNotification(candidate),
            NotificationTypes.FastingCheckInReminder => CreateEmptyNotification(candidate),
            NotificationTypes.EatingWindowStarted => CreatePhaseNotification(candidate),
            NotificationTypes.FastingWindowStarted => CreatePhaseNotification(candidate),
            _ => throw new InvalidOperationException($"Unsupported fasting notification type '{candidate.Type}'."),
        };
    }

    private static Notification CreatePhaseNotification(FastingNotificationCandidate candidate) =>
        Notification.Create(
            candidate.UserId,
            candidate.Type,
            NotificationPayloads.FastingPhase(
                candidate.PlanType ?? string.Empty,
                candidate.OccurrenceKind ?? string.Empty),
            candidate.ReferenceId);

    private static Notification CreateEmptyNotification(FastingNotificationCandidate candidate) =>
        Notification.Create(
            candidate.UserId,
            candidate.Type,
            NotificationPayloads.Empty(),
            candidate.ReferenceId);
}
