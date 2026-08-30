namespace FoodDiary.Application.WeeklyCheckIn.Models;

public sealed record WeekTrendModel(
    double CalorieChange,
    double ProteinChange,
    double FatChange,
    double CarbChange,
    double? WeightChange,
    double? WaistChange,
    int HydrationChange,
    int MealsLoggedChange);
