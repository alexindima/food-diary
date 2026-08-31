using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Common;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.SearchSuggestions;

public sealed class OpenFoodFactsProductSearchSuggestionProvider(IOpenFoodFactsCachedProductSearch cachedProductSearch) : IProductSearchSuggestionProvider {
    public string Source => "openFoodFacts";

    public async Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        IReadOnlyList<OpenFoodFactsProductModel> products = await cachedProductSearch.SearchAsync(search, limit, cancellationToken).ConfigureAwait(false);
        return products
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
            UsdaFdcId: null,
            product.ImageUrl,
            product.CaloriesPer100G,
            product.ProteinsPer100G,
            product.FatsPer100G,
            product.CarbsPer100G,
            product.FiberPer100G);
}
