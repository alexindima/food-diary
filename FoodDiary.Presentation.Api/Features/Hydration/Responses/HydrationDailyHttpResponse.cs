namespace FoodDiary.Presentation.Api.Features.Hydration.Responses;

public sealed record HydrationDailyHttpResponse(
    DateTime DateUtc,
    int TotalMl,
    double? GoalMl);
