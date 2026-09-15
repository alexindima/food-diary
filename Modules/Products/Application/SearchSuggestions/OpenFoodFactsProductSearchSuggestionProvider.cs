using FoodDiary.Mediator;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Queries.SearchProducts;
using FoodDiary.Modules.OpenFoodFacts.Contracts.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.SearchSuggestions;

public sealed class OpenFoodFactsProductSearchSuggestionProvider(ISender cachedProductSearch) : IProductSearchSuggestionProvider {
    public string Source => "openFoodFacts";

    public async Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        IReadOnlyList<OpenFoodFactsProductModel> products = (await cachedProductSearch.Send(new SearchOpenFoodFactsQuery(search, limit), cancellationToken).ConfigureAwait(false)).Value;
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
