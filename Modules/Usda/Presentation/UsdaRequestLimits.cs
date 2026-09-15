using FoodDiary.Modules.Usda.Application.Queries.SearchUsdaFoods;

namespace FoodDiary.Modules.Usda.Presentation;

public static class UsdaRequestLimits {
    public const int MaximumSearchLength = SearchUsdaFoodsQueryValidator.MaximumSearchLength;
    public const int MinimumLimit = SearchUsdaFoodsQueryValidator.MinimumLimit;
    public const int MaximumLimit = SearchUsdaFoodsQueryValidator.MaximumLimit;
}
