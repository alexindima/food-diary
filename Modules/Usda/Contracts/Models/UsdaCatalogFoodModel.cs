namespace FoodDiary.Modules.Usda.Contracts.Models;

public sealed record UsdaCatalogFoodModel(string Description, IReadOnlyList<UsdaNutrientReadModel> Nutrients);
