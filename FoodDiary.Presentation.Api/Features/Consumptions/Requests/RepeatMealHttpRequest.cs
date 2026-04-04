namespace FoodDiary.Presentation.Api.Features.Consumptions.Requests;

public sealed record RepeatMealHttpRequest(DateTime TargetDate, string? MealType = null);
