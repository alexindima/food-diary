using FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

namespace FoodDiary.Presentation.Api.Features.OpenFoodFacts;

public static class OpenFoodFactsRequestLimits {
    public const int MaximumBarcodeLength = SearchByBarcodeQueryValidator.MaximumBarcodeLength;
    public const int MaximumSearchLength = SearchOpenFoodFactsQueryValidator.MaximumSearchLength;
    public const int MinimumLimit = SearchOpenFoodFactsQueryValidator.MinimumLimit;
    public const int MaximumLimit = SearchOpenFoodFactsQueryValidator.MaximumLimit;
}
