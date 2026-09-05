namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;

public sealed record WeekSummaryHttpResponse(
    double TotalCalories,
    double AvgDailyCalories,
    double AvgProteins,
    double AvgFats,
    double AvgCarbs,
    int MealsLogged,
    int DaysLogged,
    double? WeightStart,
    double? WeightEnd,
    double? WaistStart,
    double? WaistEnd,
    int TotalHydrationMl,
    int AvgDailyHydrationMl);
