namespace FoodDiary.Application.Hydration.Models;

public sealed record HydrationDailyModel(
    DateTime DateUtc,
    int TotalMl,
    double? GoalMl);
