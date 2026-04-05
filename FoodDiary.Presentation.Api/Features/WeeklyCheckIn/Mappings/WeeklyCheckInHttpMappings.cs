using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;

namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Mappings;

public static class WeeklyCheckInHttpMappings {
    public static GetWeeklyCheckInQuery ToQuery(this Guid userId) => new(userId);

    public static WeeklyCheckInHttpResponse ToHttpResponse(this WeeklyCheckInModel model) =>
        new(
            model.ThisWeek.ToHttpResponse(),
            model.LastWeek.ToHttpResponse(),
            model.Trends.ToHttpResponse(),
            model.Suggestions);

    private static WeekSummaryHttpResponse ToHttpResponse(this WeekSummaryModel model) =>
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

    private static WeekTrendHttpResponse ToHttpResponse(this WeekTrendModel model) =>
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
