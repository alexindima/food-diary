using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;

namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Mappings;

public static class WeeklyCheckInHttpMappings {
    extension(Guid userId) {
        public GetWeeklyCheckInQuery ToQuery() => new(userId);
    }

    extension(WeeklyCheckInModel model) {
        public WeeklyCheckInHttpResponse ToHttpResponse() =>
                new(
                    model.ThisWeek.ToHttpResponse(),
                    model.LastWeek.ToHttpResponse(),
                    model.Trends.ToHttpResponse(),
                    model.Suggestions);
    }

    extension(WeekSummaryModel model) {
        private WeekSummaryHttpResponse ToHttpResponse() =>
                new(
                    model.TotalCalories,
                    model.AvgDailyCalories,
                    model.AvgProteins,
                    model.AvgFats,
                    model.AvgCarbs,
                    model.MealsLogged,
                    model.DaysLogged,
                    model.WeightStart,
                    model.WeightEnd,
                    model.WaistStart,
                    model.WaistEnd,
                    model.TotalHydrationMl,
                    model.AvgDailyHydrationMl);
    }

    extension(WeekTrendModel model) {
        private WeekTrendHttpResponse ToHttpResponse() =>
                new(
                    model.CalorieChange,
                    model.ProteinChange,
                    model.FatChange,
                    model.CarbChange,
                    model.WeightChange,
                    model.WaistChange,
                    model.HydrationChange,
                    model.MealsLoggedChange);
    }
}
