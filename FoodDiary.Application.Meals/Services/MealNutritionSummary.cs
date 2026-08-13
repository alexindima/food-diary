namespace FoodDiary.Application.Meals.Services;

public sealed record MealNutritionSummary(
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
