namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaNutrientReadModel(
    int NutrientId,
    string Name,
    string Unit,
    double Amount);
