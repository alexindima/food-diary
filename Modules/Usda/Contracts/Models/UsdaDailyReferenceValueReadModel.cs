namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaDailyReferenceValueReadModel(
    int NutrientId,
    double Value,
    string Unit);
