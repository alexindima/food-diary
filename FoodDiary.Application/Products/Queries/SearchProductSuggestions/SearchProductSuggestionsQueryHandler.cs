using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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

        foreach (IProductSearchSuggestionProvider provider in providers) {
            IReadOnlyList<ProductSearchSuggestionModel> providerSuggestions = await provider.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false);
            suggestions.AddRange(providerSuggestions);
        }

        return Result.Success<IReadOnlyList<ProductSearchSuggestionModel>>(suggestions);
    }
}
