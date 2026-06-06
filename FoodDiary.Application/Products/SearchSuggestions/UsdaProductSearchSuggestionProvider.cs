using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Products.SearchSuggestions;

public sealed class UsdaProductSearchSuggestionProvider(
    IUsdaFoodRepository usdaFoodRepository,
    IUsdaFoodSearchService usdaFoodSearchService) : IProductSearchSuggestionProvider {
    public string Source => "usda";

    public async Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        IReadOnlyList<UsdaFood> localFoods = await usdaFoodRepository.SearchAsync(search, limit, cancellationToken).ConfigureAwait(false);
        var foods = localFoods
            .Select(f => new UsdaFoodModel(f.FdcId, f.Description, f.FoodCategory))
            .ToList();

        if (foods.Count < limit) {
            int remaining = limit - foods.Count;
            IReadOnlyList<UsdaFoodModel> brandedFoods = await usdaFoodSearchService.SearchBrandedAsync(search, remaining, cancellationToken).ConfigureAwait(false);
            var existingIds = foods.Select(m => m.FdcId).ToHashSet();
            foods.AddRange(brandedFoods.Where(f => !existingIds.Contains(f.FdcId)));
        }

        return foods.Select(ToSuggestion).ToList();
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
