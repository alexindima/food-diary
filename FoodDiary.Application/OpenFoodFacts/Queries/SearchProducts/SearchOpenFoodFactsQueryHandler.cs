using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public class SearchOpenFoodFactsQueryHandler(
    IOpenFoodFactsService openFoodFactsService)
    : IQueryHandler<SearchOpenFoodFactsQuery, Result<IReadOnlyList<OpenFoodFactsProductModel>>> {
    public async Task<Result<IReadOnlyList<OpenFoodFactsProductModel>>> Handle(
        SearchOpenFoodFactsQuery query,
        CancellationToken cancellationToken) {
        var products = await openFoodFactsService.SearchAsync(query.Search, query.Limit, cancellationToken);
        return Result.Success(products);
    }
}
