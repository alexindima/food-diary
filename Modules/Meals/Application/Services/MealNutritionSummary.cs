namespace FoodDiary.Modules.Meals.Application.Services;

public sealed record MealNutritionSummary(
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
