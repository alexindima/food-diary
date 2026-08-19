using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

namespace FoodDiary.Presentation.Api.Features.Usda;

public static class UsdaRequestLimits {
    public const int MaximumSearchLength = SearchUsdaFoodsQueryValidator.MaximumSearchLength;
    public const int MinimumLimit = SearchUsdaFoodsQueryValidator.MinimumLimit;
    public const int MaximumLimit = SearchUsdaFoodsQueryValidator.MaximumLimit;
}
