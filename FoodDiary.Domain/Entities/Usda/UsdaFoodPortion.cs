using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// Standard serving size for a USDA food. Read-only reference data.
/// </summary>
public sealed class UsdaFoodPortion {
    public required int Id {
        get;
        init => field = DomainGuard.Positive(value, nameof(Id));
    }
    public required int FdcId {
        get;
        init => field = DomainGuard.Positive(value, nameof(FdcId));
    }
    public required double Amount {
        get;
        init => field = DomainGuard.PositiveFinite(value, nameof(Amount));
    }
    public string MeasureUnitName { get; init; } = string.Empty;
    public required double GramWeight {
        get;
        init => field = DomainGuard.PositiveFinite(value, nameof(GramWeight));
    }
    public string? PortionDescription { get; init; }
    public string? Modifier { get; init; }

    public UsdaFood Food { get; init; } = null!;
}
