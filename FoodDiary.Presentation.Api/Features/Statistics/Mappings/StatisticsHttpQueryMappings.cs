using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;

namespace FoodDiary.Presentation.Api.Features.Statistics.Mappings;

public static class StatisticsHttpQueryMappings {
    extension(GetStatisticsHttpQuery query) {
        public GetStatisticsQuery ToQuery(Guid userId) {
            return new GetStatisticsQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
        }
    }
}
