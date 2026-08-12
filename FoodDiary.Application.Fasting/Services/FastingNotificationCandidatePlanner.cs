using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingNotificationCandidatePlanner {
    public static IReadOnlyList<FastingNotificationCandidate> GetDueNotifications(
        FastingOccurrence occurrence,
        FastingPlan plan,
        IReadOnlyList<FastingCheckIn>? checkIns,
        DateTime nowUtc) {
        var notifications = new List<FastingNotificationCandidate>();

        foreach (string referenceId in FastingCheckInReminderPlanner.GetDueReferenceIds(occurrence, checkIns, nowUtc)) {
            notifications.Add(FastingNotificationCandidate.Create(
                occurrence,
                plan,
                NotificationTypes.FastingCheckInReminder,
                referenceId));
        }

        if (plan.Type == FastingPlanType.Intermittent) {
            notifications.AddRange(FastingIntermittentNotificationPlanner
                .GetDueNotifications(occurrence, plan, nowUtc)
                .Select(notification => FastingNotificationCandidate.Create(
                    occurrence,
                    plan,
                    notification.Type,
                    notification.ReferenceId)));
            return notifications;
        }

        FastingNotificationCandidate? completionNotification = GetCompletionNotification(occurrence, plan, nowUtc);
        if (completionNotification is not null) {
            notifications.Add(completionNotification);
        }

        return notifications;
    }

    private static FastingNotificationCandidate? GetCompletionNotification(
        FastingOccurrence occurrence,
        FastingPlan plan,
        DateTime nowUtc) {
        if (!occurrence.TargetHours.HasValue) {
            return null;
        }

        DateTime completionAtUtc = occurrence.StartedAtUtc.AddHours(occurrence.TargetHours.Value);
        if (completionAtUtc > nowUtc) {
            return null;
        }

        return FastingNotificationCandidate.Create(
            occurrence,
            plan,
            NotificationTypes.FastingCompleted,
            $"fasting-completed:{occurrence.Id.Value}");
    }
}
