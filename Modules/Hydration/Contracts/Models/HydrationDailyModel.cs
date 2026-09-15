namespace FoodDiary.Modules.Hydration.Contracts.Models;

public sealed record HydrationDailyModel(
    DateTime DateUtc,
    int TotalMl,
    double? GoalMl);
