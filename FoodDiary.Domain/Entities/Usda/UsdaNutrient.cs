using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// USDA nutrient definition (vitamin, mineral, macro). Read-only reference data.
/// </summary>
public sealed class UsdaNutrient {
    public const int NameMaxLength = 256;
    public const int UnitNameMaxLength = 32;

    public required int Id {
        get;
        init => field = DomainGuard.Positive(value, nameof(Id));
    }
    public required string Name {
        get;
        init => field = DomainGuard.RequiredText(value, NameMaxLength, nameof(Name));
    }
    public required string UnitName {
        get;
        init => field = DomainGuard.RequiredText(value, UnitNameMaxLength, nameof(UnitName));
    }
}
