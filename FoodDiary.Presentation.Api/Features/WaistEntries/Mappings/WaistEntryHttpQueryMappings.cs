using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistHistoryPageSummary;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpQueryMappings {
    public static GetWaistHistoryPageSummaryQuery ToQuery(this GetWaistHistoryPageSummaryHttpQuery query, Guid userId) =>
        new(userId, query.DateFrom, query.DateTo, query.QuantizationDays, query.EntriesLimit);
    public static GetLatestWaistEntryQuery ToLatestQuery(this Guid userId) => new(userId);

    public static GetWaistEntriesQuery ToQuery(this GetWaistEntriesHttpQuery query, Guid userId) {
        bool descending = !string.Equals(query.Sort, "asc", StringComparison.OrdinalIgnoreCase);
        return new GetWaistEntriesQuery(userId, query.DateFrom, query.DateTo, query.Limit, descending);
    }

    public static GetWaistSummariesQuery ToQuery(this GetWaistSummariesHttpQuery query, Guid userId) {
        return new GetWaistSummariesQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
    }
}
