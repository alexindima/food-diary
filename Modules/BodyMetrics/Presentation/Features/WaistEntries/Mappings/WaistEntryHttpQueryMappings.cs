using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistHistoryPageSummary;
using FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Requests;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Mappings;

public static class WaistEntryHttpQueryMappings {
    extension(GetWaistHistoryPageSummaryHttpQuery query) {
        public GetWaistHistoryPageSummaryQuery ToQuery(Guid userId) =>
                new(userId, query.DateFrom, query.DateTo, query.QuantizationDays, query.EntriesLimit);
    }
    extension(Guid userId) {
        public GetLatestWaistEntryQuery ToLatestQuery() => new(userId);
    }

    extension(GetWaistEntriesHttpQuery query) {
        public GetWaistEntriesQuery ToQuery(Guid userId) {
            bool descending = !string.Equals(query.Sort, "asc", StringComparison.OrdinalIgnoreCase);
            return new GetWaistEntriesQuery(userId, query.DateFrom, query.DateTo, query.Limit, descending);
        }
    }

    extension(GetWaistSummariesHttpQuery query) {
        public GetWaistSummariesQuery ToQuery(Guid userId) {
            return new GetWaistSummariesQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
        }
    }
}
