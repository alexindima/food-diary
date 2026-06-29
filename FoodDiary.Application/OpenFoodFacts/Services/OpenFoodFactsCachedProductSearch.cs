using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Common;

namespace FoodDiary.Application.OpenFoodFacts.Services;

internal sealed class OpenFoodFactsCachedProductSearch(
    IOpenFoodFactsService openFoodFactsService,
    IOpenFoodFactsProductCacheRepository productCacheRepository,
    IUnitOfWork unitOfWork) : IOpenFoodFactsCachedProductSearch {
    public async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<OpenFoodFactsProductModel> cachedProducts = await productCacheRepository.SearchAsync(search, limit, cancellationToken).ConfigureAwait(false);
        if (cachedProducts.Count >= limit) {
            return cachedProducts;
        }

        IReadOnlyList<OpenFoodFactsProductModel> externalProducts = await openFoodFactsService.SearchAsync(search, limit, cancellationToken).ConfigureAwait(false);
        if (externalProducts.Count > 0) {
            await productCacheRepository.UpsertAsync(externalProducts, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return externalProducts
            .Concat(cachedProducts)
            .DistinctBy(product => product.Barcode)
            .Take(limit)
            .ToList();
    }
}
