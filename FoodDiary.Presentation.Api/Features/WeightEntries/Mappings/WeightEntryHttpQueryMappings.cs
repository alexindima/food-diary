using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightHistoryPageSummary;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpQueryMappings {
    extension(GetWeightHistoryPageSummaryHttpQuery query) {
        public GetWeightHistoryPageSummaryQuery ToQuery(Guid userId) =>
                new(userId, query.DateFrom, query.DateTo, query.QuantizationDays, query.EntriesLimit);
    }
    extension(Guid userId) {
        public GetLatestWeightEntryQuery ToLatestQuery() => new(userId);
    }

    extension(GetWeightEntriesHttpQuery query) {
        public GetWeightEntriesQuery ToQuery(Guid userId) {
            bool descending = !string.Equals(query.Sort, "asc", StringComparison.OrdinalIgnoreCase);
            return new GetWeightEntriesQuery(userId, query.DateFrom, query.DateTo, query.Limit, descending);
        }
    }

    extension(GetWeightSummariesHttpQuery query) {
        public GetWeightSummariesQuery ToQuery(Guid userId) {
            return new GetWeightSummariesQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
        }
    }
}
