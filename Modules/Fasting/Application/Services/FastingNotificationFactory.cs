using FoodDiary.Modules.Notifications.Contracts.Common;

namespace FoodDiary.Modules.Fasting.Application.Services;

internal static class FastingNotificationFactory {
    public static NotificationRequest Create(FastingNotificationCandidate candidate) {
        return candidate.Type switch {
            NotificationTypes.FastingCompleted => CreatePhaseNotification(candidate),
            NotificationTypes.FastingCheckInReminder => CreateEmptyNotification(candidate),
            NotificationTypes.EatingWindowStarted => CreatePhaseNotification(candidate),
            NotificationTypes.FastingWindowStarted => CreatePhaseNotification(candidate),
            _ => throw new InvalidOperationException($"Unsupported fasting notification type '{candidate.Type}'."),
        };
    }

    private static NotificationRequest CreatePhaseNotification(FastingNotificationCandidate candidate) =>
        new(
            candidate.UserId,
            candidate.Type,
            NotificationPayloads.FastingPhase(
                candidate.PlanType ?? string.Empty,
                candidate.OccurrenceKind ?? string.Empty),
            candidate.ReferenceId);

    private static NotificationRequest CreateEmptyNotification(FastingNotificationCandidate candidate) =>
        new(
            candidate.UserId,
            candidate.Type,
            NotificationPayloads.Empty(),
            candidate.ReferenceId);
}
