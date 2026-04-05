namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// USDA nutrient definition (vitamin, mineral, macro). Read-only reference data.
/// </summary>
public sealed class UsdaNutrient {
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
}
