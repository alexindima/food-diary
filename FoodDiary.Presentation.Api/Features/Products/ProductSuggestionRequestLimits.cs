using FoodDiary.Application.Products.Products.Queries.SearchProductSuggestions;

namespace FoodDiary.Presentation.Api.Features.Products;

public static class ProductSuggestionRequestLimits {
    public const int MaximumSearchLength = SearchProductSuggestionsQueryValidator.MaximumSearchLength;
    public const int MinimumLimit = SearchProductSuggestionsQueryValidator.MinimumLimit;
    public const int MaximumLimit = SearchProductSuggestionsQueryValidator.MaximumLimit;
}
