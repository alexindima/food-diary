namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaNutrientReadModel(
    int NutrientId,
    string Name,
    string Unit,
    double Amount);
