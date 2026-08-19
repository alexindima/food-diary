using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// USDA nutrient definition (vitamin, mineral, macro). Read-only reference data.
/// </summary>
public sealed class UsdaNutrient {
    public required int Id {
        get;
        init => field = DomainGuard.Positive(value, nameof(Id));
    }
    public string Name { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
}
