namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// FDA Daily Reference Value for a nutrient. Used to compute % Daily Value.
/// </summary>
public sealed class DailyReferenceValue {
    public int Id { get; init; }
    public int NutrientId { get; init; }
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public string AgeGroup { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;

    public UsdaNutrient Nutrient { get; init; } = null!;
}
