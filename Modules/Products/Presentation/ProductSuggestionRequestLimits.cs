using FoodDiary.Modules.Products.Application.Queries.SearchProductSuggestions;

namespace FoodDiary.Modules.Products.Presentation;

public static class ProductSuggestionRequestLimits {
    public const int MaximumSearchLength = SearchProductSuggestionsQueryValidator.MaximumSearchLength;
    public const int MinimumLimit = SearchProductSuggestionsQueryValidator.MinimumLimit;
    public const int MaximumLimit = SearchProductSuggestionsQueryValidator.MaximumLimit;
}
