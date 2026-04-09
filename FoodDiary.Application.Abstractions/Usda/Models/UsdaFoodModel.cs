namespace FoodDiary.Application.Usda.Models;

public sealed record UsdaFoodModel(
    int FdcId,
    string Description,
    string? FoodCategory);
