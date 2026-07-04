using System.Globalization;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingIntermittentNotificationPlanner {
    public static IReadOnlyList<FastingWindowNotificationPlan> GetDueNotifications(
        FastingOccurrence occurrence,
        FastingPlan plan,
        DateTime nowUtc) {
        int? fastHours = plan.IntermittentFastHours ?? occurrence.TargetHours;
        int? eatingWindowHours = plan.IntermittentEatingWindowHours;
        if (!fastHours.HasValue || !eatingWindowHours.HasValue) {
            return [];
        }

        TimeSpan elapsed = nowUtc - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return [];
        }

        var notifications = new List<FastingWindowNotificationPlan>();
        int cycleLengthHours = fastHours.Value + eatingWindowHours.Value;
        int completedCycles = (int)Math.Floor(elapsed.TotalHours / cycleLengthHours);
        for (int cycleIndex = 0; cycleIndex <= completedCycles; cycleIndex++) {
            AddEatingWindowNotification(occurrence, nowUtc, fastHours.Value, cycleLengthHours, cycleIndex, notifications);
            AddFastingWindowNotification(occurrence, nowUtc, cycleLengthHours, cycleIndex, notifications);
        }

        return notifications;
    }

    private static void AddEatingWindowNotification(
        FastingOccurrence occurrence,
        DateTime nowUtc,
        int fastHours,
        int cycleLengthHours,
        int cycleIndex,
        ICollection<FastingWindowNotificationPlan> notifications) {
        DateTime eatingWindowStartUtc = occurrence.StartedAtUtc.AddHours((cycleIndex * cycleLengthHours) + fastHours);
        if (eatingWindowStartUtc > nowUtc) {
            return;
        }

        notifications.Add(new FastingWindowNotificationPlan(
            NotificationTypes.EatingWindowStarted,
            string.Create(CultureInfo.InvariantCulture, $"eating-window-started:{occurrence.Id.Value}:{cycleIndex + 1}")));
    }

    private static void AddFastingWindowNotification(
        FastingOccurrence occurrence,
        DateTime nowUtc,
        int cycleLengthHours,
        int cycleIndex,
        ICollection<FastingWindowNotificationPlan> notifications) {
        DateTime fastingWindowStartUtc = occurrence.StartedAtUtc.AddHours((cycleIndex + 1) * cycleLengthHours);
        if (fastingWindowStartUtc > nowUtc) {
            return;
        }

        notifications.Add(new FastingWindowNotificationPlan(
            NotificationTypes.FastingWindowStarted,
            $"fasting-window-started:{occurrence.Id.Value}:{(cycleIndex + 2).ToString(CultureInfo.InvariantCulture)}"));
    }
}
