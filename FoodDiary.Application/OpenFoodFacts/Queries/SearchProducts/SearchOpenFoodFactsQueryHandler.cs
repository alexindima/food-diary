using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;

namespace FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;

public class SearchOpenFoodFactsQueryHandler(
    IOpenFoodFactsService openFoodFactsService,
    IOpenFoodFactsProductCacheRepository productCacheRepository,
    IUnitOfWork unitOfWork)
    : IQueryHandler<SearchOpenFoodFactsQuery, Result<IReadOnlyList<OpenFoodFactsProductModel>>> {
    public async Task<Result<IReadOnlyList<OpenFoodFactsProductModel>>> Handle(
        SearchOpenFoodFactsQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<OpenFoodFactsProductModel> cachedProducts = await productCacheRepository.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false);
        if (cachedProducts.Count >= query.Limit) {
            return Result.Success(cachedProducts);
        }

        IReadOnlyList<OpenFoodFactsProductModel> externalProducts = await openFoodFactsService.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false);
        if (externalProducts.Count > 0) {
            await productCacheRepository.UpsertAsync(externalProducts, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var products = externalProducts
            .Concat(cachedProducts)
            .DistinctBy(product => product.Barcode)
            .Take(query.Limit)
            .ToList();

        return Result.Success<IReadOnlyList<OpenFoodFactsProductModel>>(products);
    }
}
