using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.Usda;

/// <summary>
/// USDA FoodData Central SR Legacy food item. Read-only reference data.
/// </summary>
public sealed class UsdaFood {
    private readonly List<UsdaFoodNutrient> _foodNutrients = [];
    private readonly List<UsdaFoodPortion> _foodPortions = [];

    public required int FdcId {
        get;
        init => field = DomainGuard.Positive(value, nameof(FdcId));
    }
    public string Description { get; init; } = string.Empty;
    public int? FoodCategoryId {
        get;
        init => field = DomainGuard.Positive(value, nameof(FoodCategoryId));
    }
    public string? FoodCategory { get; init; }

    public IReadOnlyCollection<UsdaFoodNutrient> FoodNutrients => _foodNutrients.AsReadOnly();
    public IReadOnlyCollection<UsdaFoodPortion> FoodPortions => _foodPortions.AsReadOnly();
}
