using FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchByBarcode;
using FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchProducts;

namespace FoodDiary.Modules.OpenFoodFacts.Presentation;

public static class OpenFoodFactsRequestLimits {
    public const int MaximumBarcodeLength = SearchByBarcodeQueryValidator.MaximumBarcodeLength;
    public const int MaximumSearchLength = SearchOpenFoodFactsQueryValidator.MaximumSearchLength;
    public const int MinimumLimit = SearchOpenFoodFactsQueryValidator.MinimumLimit;
    public const int MaximumLimit = SearchOpenFoodFactsQueryValidator.MaximumLimit;
}
