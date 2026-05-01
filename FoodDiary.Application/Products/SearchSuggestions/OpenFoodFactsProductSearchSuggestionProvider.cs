using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.SearchSuggestions;

public sealed class OpenFoodFactsProductSearchSuggestionProvider(
    IOpenFoodFactsService openFoodFactsService,
    IOpenFoodFactsProductCacheRepository productCacheRepository) : IProductSearchSuggestionProvider {
    public string Source => "openFoodFacts";

    public async Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        var cachedProducts = await productCacheRepository.SearchAsync(search, limit, cancellationToken);
        if (cachedProducts.Count >= limit) {
            return cachedProducts.Select(ToSuggestion).ToList();
        }

        var externalProducts = await openFoodFactsService.SearchAsync(search, limit, cancellationToken);
        if (externalProducts.Count > 0) {
            await productCacheRepository.UpsertAsync(externalProducts, cancellationToken);
        }

        return externalProducts
            .Concat(cachedProducts)
            .DistinctBy(product => product.Barcode)
            .Take(limit)
            .Select(ToSuggestion)
            .ToList();
    }

    private ProductSearchSuggestionModel ToSuggestion(OpenFoodFactsProductModel product) =>
        new(
            Source,
            product.Name,
            product.Brand,
            product.Category,
            product.Barcode,
            null,
            product.ImageUrl,
            product.CaloriesPer100G,
            product.ProteinsPer100G,
            product.FatsPer100G,
            product.CarbsPer100G,
            product.FiberPer100G);
}
