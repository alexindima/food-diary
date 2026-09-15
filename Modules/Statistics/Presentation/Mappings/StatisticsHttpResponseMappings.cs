using FoodDiary.Modules.BodyMetrics.Presentation.Mappings.Features.WeightEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Presentation.Mappings.Features.WaistEntries.Mappings;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Presentation.Responses;

namespace FoodDiary.Modules.Statistics.Presentation.Mappings;

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

    extension(StatisticsSummaryModel model) {
        public StatisticsSummaryHttpResponse ToHttpResponse() {
            return new StatisticsSummaryHttpResponse(
                [.. model.Nutrition.Select(static item => item.ToHttpResponse())],
                [.. model.Weight.Select(static item => item.ToHttpResponse())],
                [.. model.Waist.Select(static item => item.ToHttpResponse())]);
        }
    }
}
