using FoodDiary.Application.Statistics.Models;
using FoodDiary.Presentation.Api.Features.Statistics.Responses;

namespace FoodDiary.Presentation.Api.Features.Statistics.Mappings;

public static class StatisticsHttpResponseMappings {
    extension(AggregatedStatisticsModel model) {
        public AggregatedStatisticsHttpResponse ToHttpResponse() {
            return new AggregatedStatisticsHttpResponse(
                model.DateFrom,
                model.DateTo,
                model.TotalCalories,
                model.AverageProteins,
                model.AverageFats,
                model.AverageCarbs,
                model.AverageFiber,
                model.TotalProteins,
                model.TotalFats,
                model.TotalCarbs,
                model.TotalFiber,
                model.BreakfastCalories,
                model.LunchCalories,
                model.DinnerCalories,
                model.SnackCalories,
                model.MealCount,
                model.TrackedDayCount
            );
        }
    }
}
