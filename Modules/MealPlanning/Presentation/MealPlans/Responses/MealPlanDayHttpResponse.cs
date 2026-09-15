namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Responses;

public sealed record MealPlanDayHttpResponse(
    Guid Id,
    int DayNumber,
    IReadOnlyList<MealPlanMealHttpResponse> Meals);
