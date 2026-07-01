namespace FoodDiary.Application.Consumptions.Services;

internal readonly record struct ConsumptionNutritionInput(
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol);
