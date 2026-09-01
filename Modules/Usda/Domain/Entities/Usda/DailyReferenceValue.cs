namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// FDA Daily Reference Value for a nutrient. Used to compute % Daily Value.
/// </summary>
public sealed class DailyReferenceValue {
    public const int UnitMaxLength = 32;
    public const int AgeGroupMaxLength = 64;
    public const int GenderMaxLength = 16;

    public int Id {
        get;
        init => field = UsdaDomainGuard.Positive(value, nameof(Id));
    }
    public required int NutrientId {
        get;
        init => field = UsdaDomainGuard.Positive(value, nameof(NutrientId));
    }
    public required double Value {
        get;
        init => field = UsdaDomainGuard.PositiveFinite(value, nameof(Value));
    }
    public required string Unit {
        get;
        init => field = UsdaDomainGuard.RequiredText(value, UnitMaxLength, nameof(Unit));
    }
    public required string AgeGroup {
        get;
        init => field = UsdaDomainGuard.RequiredText(value, AgeGroupMaxLength, nameof(AgeGroup));
    }
    public required string Gender {
        get;
        init => field = UsdaDomainGuard.RequiredText(value, GenderMaxLength, nameof(Gender));
    }

    public UsdaNutrient Nutrient { get; init; } = null!;
}
