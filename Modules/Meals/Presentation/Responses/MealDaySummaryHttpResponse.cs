namespace FoodDiary.Modules.Meals.Presentation.Responses;

public sealed record MealDaySummaryHttpResponse(DateOnly Date, double TotalCalories, int MealCount);
