using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using System.Globalization;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Services;

internal static class FastingCheckInReminderPlanner {
    public static IReadOnlyList<string> GetDueReferenceIds(
        FastingOccurrence occurrence,
        IReadOnlyList<FastingCheckIn>? checkIns,
        DateTime nowUtc,
        FastingReminderSchedule reminderDelays) {
        if (HasExistingCheckIn(occurrence, checkIns)) {
            return [];
        }

        TimeSpan elapsed = nowUtc - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return [];
        }

        return [.. reminderDelays.DueHours
            .Where(hour => elapsed.TotalHours >= hour)
            .Select(hour => string.Create(
                CultureInfo.InvariantCulture,
                $"fasting-check-in-reminder:{occurrence.Id.Value}:{hour}"))];
    }

    private static bool HasExistingCheckIn(FastingOccurrence occurrence, IReadOnlyList<FastingCheckIn>? checkIns) =>
        checkIns is { Count: > 0 } || occurrence.CheckInAtUtc.HasValue;
}
