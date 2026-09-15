namespace FoodDiary.Modules.Meals.Application.Common;

public sealed record ManualNutritionInput(
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
