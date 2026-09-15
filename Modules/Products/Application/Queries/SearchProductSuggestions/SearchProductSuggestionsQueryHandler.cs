using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.Queries.SearchProductSuggestions;

public sealed class SearchProductSuggestionsQueryHandler(IEnumerable<IProductSearchSuggestionProvider> providers)
    : IQueryHandler<SearchProductSuggestionsQuery, Result<IReadOnlyList<ProductSearchSuggestionModel>>> {
    public async Task<Result<IReadOnlyList<ProductSearchSuggestionModel>>> Handle(
        SearchProductSuggestionsQuery query,
        CancellationToken cancellationToken) {
        var suggestions = new List<ProductSearchSuggestionModel>();

        foreach (IProductSearchSuggestionProvider provider in providers) {
            int remaining = query.Limit - suggestions.Count;
            if (remaining <= 0) {
                break;
            }

            IReadOnlyList<ProductSearchSuggestionModel> providerSuggestions = await provider
                .SearchAsync(query.Search, remaining, cancellationToken)
                .ConfigureAwait(false);
            suggestions.AddRange(providerSuggestions);
        }

        return Result.Success<IReadOnlyList<ProductSearchSuggestionModel>>([.. suggestions.Take(query.Limit)]);
    }
}
