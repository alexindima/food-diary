namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;

public sealed record WeeklyCheckInHttpResponse(
    WeekSummaryHttpResponse ThisWeek,
    WeekSummaryHttpResponse LastWeek,
    WeekTrendHttpResponse Trends,
    IReadOnlyList<string> Suggestions);

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

public sealed record WeekTrendHttpResponse(
    double CalorieChange,
    double ProteinChange,
    double FatChange,
    double CarbChange,
    double? WeightChange,
    double? WaistChange,
    int HydrationChange,
    int MealsLoggedChange);
