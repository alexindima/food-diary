using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpQueryMappings {
    public static GetWaistEntriesQuery ToQuery(this GetWaistEntriesHttpQuery query, UserId userId) {
        var descending = !string.Equals(query.Sort, "asc", StringComparison.OrdinalIgnoreCase);
        return new GetWaistEntriesQuery(userId, query.DateFrom, query.DateTo, query.Limit, descending);
    }

    public static GetWaistSummariesQuery ToQuery(this GetWaistSummariesHttpQuery query, UserId userId) {
        return new GetWaistSummariesQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
    }
}
