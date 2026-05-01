using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public class SearchOpenFoodFactsQueryHandler(
    IOpenFoodFactsService openFoodFactsService,
    IOpenFoodFactsProductCacheRepository productCacheRepository)
    : IQueryHandler<SearchOpenFoodFactsQuery, Result<IReadOnlyList<OpenFoodFactsProductModel>>> {
    public async Task<Result<IReadOnlyList<OpenFoodFactsProductModel>>> Handle(
        SearchOpenFoodFactsQuery query,
        CancellationToken cancellationToken) {
        var cachedProducts = await productCacheRepository.SearchAsync(query.Search, query.Limit, cancellationToken);
        if (cachedProducts.Count >= query.Limit) {
            return Result.Success(cachedProducts);
        }

        var externalProducts = await openFoodFactsService.SearchAsync(query.Search, query.Limit, cancellationToken);
        if (externalProducts.Count > 0) {
            await productCacheRepository.UpsertAsync(externalProducts, cancellationToken);
        }

        var products = externalProducts
            .Concat(cachedProducts)
            .DistinctBy(product => product.Barcode)
            .Take(query.Limit)
            .ToList();

        return Result.Success<IReadOnlyList<OpenFoodFactsProductModel>>(products);
    }
}
