namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaMealProductNutritionReadModel(
    double Amount,
    double ProductBaseAmount,
    int? UsdaFdcId);
