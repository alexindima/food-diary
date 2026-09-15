namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaMealProductNutritionReadModel(
    double Amount,
    double ProductBaseAmount,
    int? UsdaFdcId);
