using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingOccurrenceAnalysisBuilder {
    public static IReadOnlyList<FastingOccurrenceAnalysis> Build(
        IReadOnlyList<FastingOccurrenceReadModel> occurrences,
        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckInReadModel>> checkInsByOccurrence) =>
        [.. occurrences.Select(occurrence => {
            IReadOnlyList<FastingCheckInSnapshot> timeline = FastingCheckInTimelineBuilder.Build(
                occurrence,
                checkInsByOccurrence.GetValueOrDefault(occurrence.Id));
            FastingCheckInSnapshot? latestCheckIn = timeline.Count > 0 ? timeline[0] : null;
            return new FastingOccurrenceAnalysis(occurrence, timeline, latestCheckIn);
        })];
}
