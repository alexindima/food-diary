namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// Standard serving size for a USDA food. Read-only reference data.
/// </summary>
public sealed class UsdaFoodPortion {
    public int Id { get; init; }
    public int FdcId { get; init; }
    public double Amount { get; init; }
    public string MeasureUnitName { get; init; } = string.Empty;
    public double GramWeight { get; init; }
    public string? PortionDescription { get; init; }
    public string? Modifier { get; init; }

    public UsdaFood Food { get; init; } = null!;
}
