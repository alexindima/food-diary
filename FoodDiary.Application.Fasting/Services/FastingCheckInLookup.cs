using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingCheckInLookup {
    public static IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>> Create(
        IEnumerable<FastingCheckIn> checkIns) =>
        checkIns
            .GroupBy(static checkIn => checkIn.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckIn>)[.. group]);

    public static IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckInReadModel>> Create(
        IEnumerable<FastingCheckInReadModel> checkIns) =>
        checkIns
            .GroupBy(static checkIn => checkIn.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckInReadModel>)[.. group]);
}
