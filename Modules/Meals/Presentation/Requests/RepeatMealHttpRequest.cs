namespace FoodDiary.Modules.Meals.Presentation.Requests;

public sealed record RepeatMealHttpRequest(DateTime TargetDate, string? MealType = null);
