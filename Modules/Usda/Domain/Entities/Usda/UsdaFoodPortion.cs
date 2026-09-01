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
        init => field = UsdaDomainGuard.Positive(value, nameof(Id));
    }
    public required int FdcId {
        get;
        init => field = UsdaDomainGuard.Positive(value, nameof(FdcId));
    }
    public required double Amount {
        get;
        init => field = UsdaDomainGuard.PositiveFinite(value, nameof(Amount));
    }
    public required string MeasureUnitName {
        get;
        init => field = UsdaDomainGuard.RequiredText(value, MeasureUnitNameMaxLength, nameof(MeasureUnitName));
    }
    public required double GramWeight {
        get;
        init => field = UsdaDomainGuard.PositiveFinite(value, nameof(GramWeight));
    }
    public string? PortionDescription {
        get;
        init => field = UsdaDomainGuard.OptionalText(value, PortionDescriptionMaxLength, nameof(PortionDescription));
    }
    public string? Modifier {
        get;
        init => field = UsdaDomainGuard.OptionalText(value, ModifierMaxLength, nameof(Modifier));
    }

    public UsdaFood Food { get; init; } = null!;
}
