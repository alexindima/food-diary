using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// Standard serving size for a USDA food. Read-only reference data.
/// </summary>
public sealed class UsdaFoodPortion {
    public const int MeasureUnitNameMaxLength = 128;
    public const int PortionDescriptionMaxLength = 256;
    public const int ModifierMaxLength = 128;

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
    public required string MeasureUnitName {
        get;
        init => field = DomainGuard.RequiredText(value, MeasureUnitNameMaxLength, nameof(MeasureUnitName));
    }
    public required double GramWeight {
        get;
        init => field = DomainGuard.PositiveFinite(value, nameof(GramWeight));
    }
    public string? PortionDescription {
        get;
        init => field = DomainGuard.OptionalText(value, PortionDescriptionMaxLength, nameof(PortionDescription));
    }
    public string? Modifier {
        get;
        init => field = DomainGuard.OptionalText(value, ModifierMaxLength, nameof(Modifier));
    }

    public UsdaFood Food { get; init; } = null!;
}
