namespace FoodDiary.Application.Meals.Services;

internal readonly record struct MealNutritionInput(
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol);
