using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpQueryMappings {
    public static GetWeightEntriesQuery ToQuery(this GetWeightEntriesHttpQuery query, UserId userId) {
        var descending = !string.Equals(query.Sort, "asc", StringComparison.OrdinalIgnoreCase);
        return new GetWeightEntriesQuery(userId, query.DateFrom, query.DateTo, query.Limit, descending);
    }

    public static GetWeightSummariesQuery ToQuery(this GetWeightSummariesHttpQuery query, UserId userId) {
        return new GetWeightSummariesQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
    }
}
