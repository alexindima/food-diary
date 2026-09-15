namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaFoodModel(
    int FdcId,
    string Description,
    string? FoodCategory);
