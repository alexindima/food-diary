using System.Globalization;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingCheckInReminderPlanner {
    public static IReadOnlyList<string> GetDueReferenceIds(
        FastingOccurrence occurrence,
        IReadOnlyList<FastingCheckIn>? checkIns,
        DateTime nowUtc) {
        if (HasExistingCheckIn(occurrence, checkIns)) {
            return [];
        }

        TimeSpan elapsed = nowUtc - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return [];
        }

        return [.. new[] {
                occurrence.User.FastingCheckInReminderHours,
                occurrence.User.FastingCheckInFollowUpReminderHours,
            }
            .Distinct()
            .Order()
            .Where(hour => elapsed.TotalHours >= hour)
            .Select(hour => string.Create(
                CultureInfo.InvariantCulture,
                $"fasting-check-in-reminder:{occurrence.Id.Value}:{hour}"))];
    }

    private static bool HasExistingCheckIn(FastingOccurrence occurrence, IReadOnlyList<FastingCheckIn>? checkIns) =>
        checkIns is { Count: > 0 } || occurrence.CheckInAtUtc.HasValue;
}
