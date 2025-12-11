namespace FoodDiary.Contracts.Hydration;

public record HydrationDailyResponse(
    DateTime DateUtc,
    int TotalMl,
    double? GoalMl);
