using FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.OpenFoodFacts.Application.Abstractions.Common;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.OpenFoodFacts.Application.Queries.SearchProducts;

public sealed class SearchOpenFoodFactsQueryHandler(
    IOpenFoodFactsService openFoodFactsService,
    IOpenFoodFactsProductCacheReadRepository productCacheReadRepository,
    IOpenFoodFactsProductCacheWriteRepository productCacheWriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SearchOpenFoodFactsQuery, Result<IReadOnlyList<OpenFoodFactsProductModel>>> {
    public async Task<Result<IReadOnlyList<OpenFoodFactsProductModel>>> Handle(
        SearchOpenFoodFactsQuery request,
        CancellationToken cancellationToken) {
        IReadOnlyList<OpenFoodFactsProductModel> products = await SearchAsync(request.Search, request.Limit, cancellationToken).ConfigureAwait(false);
        return Result.Success(products);
    }
    private async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<OpenFoodFactsProductModel> cachedProducts = await productCacheReadRepository.SearchAsync(search, limit, cancellationToken).ConfigureAwait(false);
        if (cachedProducts.Count >= limit) {
            return cachedProducts;
        }

        IReadOnlyList<OpenFoodFactsProductModel> externalProducts = await openFoodFactsService.SearchAsync(search, limit, cancellationToken).ConfigureAwait(false);
        if (externalProducts.Count > 0) {
            await productCacheWriteRepository.UpsertAsync(externalProducts, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return externalProducts
            .Concat(cachedProducts)
            .DistinctBy(product => product.Barcode, StringComparer.Ordinal)
            .Take(limit)
            .ToList();
    }
}
