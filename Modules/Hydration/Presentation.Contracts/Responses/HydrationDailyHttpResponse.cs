namespace FoodDiary.Modules.Hydration.Presentation.Contracts.Responses;

public sealed record HydrationDailyHttpResponse(
    DateTime DateUtc,
    int TotalMl,
    double? GoalMl);
