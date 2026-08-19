using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// Amount of a specific nutrient in a USDA food (per 100g). Read-only reference data.
/// </summary>
public sealed class UsdaFoodNutrient {
    public required int Id {
        get;
        init => field = DomainGuard.Positive(value, nameof(Id));
    }
    public required int FdcId {
        get;
        init => field = DomainGuard.Positive(value, nameof(FdcId));
    }
    public required int NutrientId {
        get;
        init => field = DomainGuard.Positive(value, nameof(NutrientId));
    }
    public required double Amount {
        get;
        init => field = DomainGuard.NonNegativeFinite(value, nameof(Amount));
    }

    public UsdaFood Food { get; init; } = null!;
    public UsdaNutrient Nutrient { get; init; } = null!;
}
