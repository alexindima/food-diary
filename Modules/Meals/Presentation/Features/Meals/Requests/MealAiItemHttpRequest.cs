namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record MealAiItemHttpRequest(
    string NameEn,
    string? NameLocal,
    double Amount,
    string Unit,
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol,
    double? Confidence = null,
    string? Resolution = null);
