namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record RepeatMealHttpRequest(DateTime TargetDate, string? MealType = null);
