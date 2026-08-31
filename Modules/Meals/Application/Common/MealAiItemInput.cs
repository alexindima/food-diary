namespace FoodDiary.Application.Meals.Common;

public record MealAiItemInput(
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
