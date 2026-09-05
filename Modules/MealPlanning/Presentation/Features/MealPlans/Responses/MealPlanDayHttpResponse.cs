namespace FoodDiary.Presentation.Api.Features.MealPlans.Responses;

public sealed record MealPlanDayHttpResponse(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealHttpResponse> Meals);
