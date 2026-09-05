using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Application.Statistics.Queries.GetStatisticsSummary;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;

namespace FoodDiary.Presentation.Api.Features.Statistics.Mappings;

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
