using FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistHistoryPageSummary;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

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
