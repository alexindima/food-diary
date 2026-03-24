using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpQueryMappings {
    public static GetLatestWeightEntryQuery ToLatestQuery(this Guid userId) => new(userId);

    public static GetWeightEntriesQuery ToQuery(this GetWeightEntriesHttpQuery query, Guid userId) {
        var descending = !string.Equals(query.Sort, "asc", StringComparison.OrdinalIgnoreCase);
        return new GetWeightEntriesQuery(userId, query.DateFrom, query.DateTo, query.Limit, descending);
    }

    public static GetWeightSummariesQuery ToQuery(this GetWeightSummariesHttpQuery query, Guid userId) {
        return new GetWeightSummariesQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
    }
}
