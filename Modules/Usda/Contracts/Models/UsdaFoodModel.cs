namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaFoodModel(
    int FdcId,
    string Description,
    string? FoodCategory);
