namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// USDA FoodData Central SR Legacy food item. Read-only reference data.
/// </summary>
public sealed class UsdaFood {
    public int FdcId { get; init; }
    public string Description { get; init; } = string.Empty;
    public int? FoodCategoryId { get; init; }
    public string? FoodCategory { get; init; }

    public ICollection<UsdaFoodNutrient> FoodNutrients { get; init; } = [];
    public ICollection<UsdaFoodPortion> FoodPortions { get; init; } = [];
}
