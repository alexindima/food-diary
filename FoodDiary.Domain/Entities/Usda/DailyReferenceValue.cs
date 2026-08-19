using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// FDA Daily Reference Value for a nutrient. Used to compute % Daily Value.
/// </summary>
public sealed class DailyReferenceValue {
    public int Id {
        get;
        init => field = DomainGuard.Positive(value, nameof(Id));
    }
    public required int NutrientId {
        get;
        init => field = DomainGuard.Positive(value, nameof(NutrientId));
    }
    public required double Value {
        get;
        init => field = DomainGuard.PositiveFinite(value, nameof(Value));
    }
    public string Unit { get; init; } = string.Empty;
    public string AgeGroup { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;

    public UsdaNutrient Nutrient { get; init; } = null!;
}
