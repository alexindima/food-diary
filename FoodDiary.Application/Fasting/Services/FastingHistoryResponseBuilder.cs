using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Mappings;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingHistoryResponseBuilder {
    public static PagedResponse<FastingSessionModel> Build(
        IReadOnlyList<FastingOccurrence> occurrences,
        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>> checkInsByOccurrence,
        int page,
        int limit,
        int totalItems) {
        var models = occurrences
            .Select(occurrence => occurrence.ToModel(
                occurrence.Plan,
                checkInsByOccurrence.GetValueOrDefault(occurrence.Id)))
            .ToList();
        int totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)limit);

        return new PagedResponse<FastingSessionModel>(models, page, limit, totalPages, totalItems);
    }
}
