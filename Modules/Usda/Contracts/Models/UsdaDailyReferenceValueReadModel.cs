namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaDailyReferenceValueReadModel(
    int NutrientId,
    double Value,
    string Unit);
