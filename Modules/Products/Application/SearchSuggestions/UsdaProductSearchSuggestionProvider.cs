using FoodDiary.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Application.Models;

namespace FoodDiary.Modules.Products.Application.SearchSuggestions;

public sealed class UsdaProductSearchSuggestionProvider(
    ISender sender) : IProductSearchSuggestionProvider {
    public string Source => "usda";

    public async Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        Result<IReadOnlyList<UsdaFoodModel>> result = await sender.Send(new SearchUsdaFoodsQuery(search, limit), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? result.Value.Select(ToSuggestion).ToArray() : [];
    }

    private ProductSearchSuggestionModel ToSuggestion(UsdaFoodModel food) =>
        new(
            Source,
            food.Description,
            Brand: null,
            food.FoodCategory,
            Barcode: null,
            food.FdcId,
            ImageUrl: null,
            CaloriesPer100G: null,
            ProteinsPer100G: null,
            FatsPer100G: null,
            CarbsPer100G: null,
            FiberPer100G: null);
}
