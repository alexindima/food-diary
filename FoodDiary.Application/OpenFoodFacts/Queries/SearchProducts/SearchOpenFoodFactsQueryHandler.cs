using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

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
