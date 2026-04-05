namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// Amount of a specific nutrient in a USDA food (per 100g). Read-only reference data.
/// </summary>
public sealed class UsdaFoodNutrient {
    public int Id { get; init; }
    public int FdcId { get; init; }
    public int NutrientId { get; init; }
    public double Amount { get; init; }

    public UsdaFood Food { get; init; } = null!;
    public UsdaNutrient Nutrient { get; init; } = null!;
}
