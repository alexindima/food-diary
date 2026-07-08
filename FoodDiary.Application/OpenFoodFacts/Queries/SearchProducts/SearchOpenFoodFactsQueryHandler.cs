using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Common;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public sealed class SearchOpenFoodFactsQueryHandler(IOpenFoodFactsCachedProductSearch cachedProductSearch)
    : IQueryHandler<SearchOpenFoodFactsQuery, Result<IReadOnlyList<OpenFoodFactsProductModel>>> {
    public async Task<Result<IReadOnlyList<OpenFoodFactsProductModel>>> Handle(
        SearchOpenFoodFactsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<OpenFoodFactsProductModel> products = await cachedProductSearch.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false);
        return Result.Success(products);
    }
}
