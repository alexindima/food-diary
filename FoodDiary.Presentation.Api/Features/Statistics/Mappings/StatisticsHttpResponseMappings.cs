using FoodDiary.Application.Statistics.Models;
using FoodDiary.Presentation.Api.Features.Statistics.Responses;

namespace FoodDiary.Presentation.Api.Features.Statistics.Mappings;

public static class StatisticsHttpResponseMappings {
    public static AggregatedStatisticsHttpResponse ToHttpResponse(this AggregatedStatisticsModel model) {
        return new AggregatedStatisticsHttpResponse(
            model.DateFrom,
            model.DateTo,
            model.TotalCalories,
            model.AverageProteins,
            model.AverageFats,
            model.AverageCarbs,
            model.AverageFiber
        );
    }
}
