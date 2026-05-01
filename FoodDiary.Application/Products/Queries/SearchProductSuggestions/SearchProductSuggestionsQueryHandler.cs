using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.Queries.SearchProductSuggestions;

public sealed class SearchProductSuggestionsQueryHandler(IEnumerable<IProductSearchSuggestionProvider> providers)
    : IQueryHandler<SearchProductSuggestionsQuery, Result<IReadOnlyList<ProductSearchSuggestionModel>>> {
    public async Task<Result<IReadOnlyList<ProductSearchSuggestionModel>>> Handle(
        SearchProductSuggestionsQuery query,
        CancellationToken cancellationToken) {
        var suggestions = new List<ProductSearchSuggestionModel>();

        foreach (var provider in providers) {
            var providerSuggestions = await provider.SearchAsync(query.Search, query.Limit, cancellationToken);
            suggestions.AddRange(providerSuggestions);
        }

        return Result.Success<IReadOnlyList<ProductSearchSuggestionModel>>(suggestions);
    }
}
