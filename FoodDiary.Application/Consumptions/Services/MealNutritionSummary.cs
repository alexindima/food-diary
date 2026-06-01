namespace FoodDiary.Application.Consumptions.Services;

public sealed record MealNutritionSummary(
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
