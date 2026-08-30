namespace FoodDiary.Application.Abstractions.Usda.Models;

public sealed record UsdaFoodReadModel(
    int FdcId,
    string Description,
    string? FoodCategory);
