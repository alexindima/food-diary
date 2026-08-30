namespace FoodDiary.Application.WeeklyCheckIn.Models;

public sealed record WeekSummaryModel(
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
