using FoodDiary.Modules.Statistics.Application.Queries.GetStatistics;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;
using FoodDiary.Modules.Statistics.Presentation.Requests;

namespace FoodDiary.Modules.Statistics.Presentation.Mappings;

public static class StatisticsHttpQueryMappings {
    extension(GetStatisticsHttpQuery query) {
        public GetStatisticsQuery ToQuery(Guid userId) {
            return new GetStatisticsQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
        }

        public GetStatisticsSummaryQuery ToSummaryQuery(Guid userId) {
            return new GetStatisticsSummaryQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
        }
    }
}
