using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingCheckInTimelineBuilder {
    public static IReadOnlyList<FastingCheckInSnapshot> Build(
        FastingOccurrence occurrence,
        IReadOnlyList<FastingCheckIn>? checkIns) {
        if (checkIns is { Count: > 0 }) {
            return checkIns
                .OrderByDescending(static checkIn => checkIn.CheckedInAtUtc)
                .Select(static checkIn => new FastingCheckInSnapshot(
                    checkIn.CheckedInAtUtc,
                    checkIn.HungerLevel,
                    checkIn.EnergyLevel,
                    checkIn.MoodLevel,
                    ParseSymptoms(checkIn.Symptoms),
                    checkIn.Notes))
                .ToList();
        }

        if (!occurrence.CheckInAtUtc.HasValue) {
            return [];
        }

        return [
            new FastingCheckInSnapshot(
                occurrence.CheckInAtUtc.Value,
                occurrence.HungerLevel ?? 0,
                occurrence.EnergyLevel ?? 0,
                occurrence.MoodLevel ?? 0,
                ParseSymptoms(occurrence.Symptoms),
                occurrence.CheckInNotes),
        ];
    }

    private static IReadOnlyList<string> ParseSymptoms(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
}
