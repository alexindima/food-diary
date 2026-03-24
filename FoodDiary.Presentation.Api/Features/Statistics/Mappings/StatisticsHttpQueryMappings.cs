using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;

namespace FoodDiary.Presentation.Api.Features.Statistics.Mappings;

public static class StatisticsHttpQueryMappings {
    public static GetStatisticsQuery ToQuery(this GetStatisticsHttpQuery query, UserId userId) {
        return new GetStatisticsQuery(userId, query.DateFrom, query.DateTo, query.QuantizationDays);
    }
}
