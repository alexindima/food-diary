using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// USDA FoodData Central SR Legacy food item. Read-only reference data.
/// </summary>
public sealed class UsdaFood {
    public const int DescriptionMaxLength = 512;
    public const int FoodCategoryMaxLength = 256;

    private readonly List<UsdaFoodNutrient> _foodNutrients = [];
    private readonly List<UsdaFoodPortion> _foodPortions = [];

    public required int FdcId {
        get;
        init => field = DomainGuard.Positive(value, nameof(FdcId));
    }
    public required string Description {
        get;
        init => field = DomainGuard.RequiredText(value, DescriptionMaxLength, nameof(Description));
    }
    public int? FoodCategoryId {
        get;
        init => field = DomainGuard.Positive(value, nameof(FoodCategoryId));
    }
    public string? FoodCategory {
        get;
        init => field = DomainGuard.OptionalText(value, FoodCategoryMaxLength, nameof(FoodCategory));
    }

    public IReadOnlyCollection<UsdaFoodNutrient> FoodNutrients => _foodNutrients.AsReadOnly();
    public IReadOnlyCollection<UsdaFoodPortion> FoodPortions => _foodPortions.AsReadOnly();
}
