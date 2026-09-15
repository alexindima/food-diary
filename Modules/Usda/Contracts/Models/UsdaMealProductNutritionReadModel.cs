using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaMealProductNutritionReadModel(
    double Amount,
    MeasurementUnit ProductBaseUnit,
    int? UsdaFdcId);
