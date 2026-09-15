namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaFoodReadModel(
    int FdcId,
    string Description,
    string? FoodCategory);
