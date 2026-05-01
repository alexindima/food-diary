using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Products.SearchSuggestions;

public sealed class UsdaProductSearchSuggestionProvider(
    IUsdaFoodRepository usdaFoodRepository,
    IUsdaFoodSearchService usdaFoodSearchService) : IProductSearchSuggestionProvider {
    public string Source => "usda";

    public async Task<IReadOnlyList<ProductSearchSuggestionModel>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        var localFoods = await usdaFoodRepository.SearchAsync(search, limit, cancellationToken);
        var foods = localFoods
            .Select(f => new UsdaFoodModel(f.FdcId, f.Description, f.FoodCategory))
            .ToList();

        if (foods.Count < limit) {
            var remaining = limit - foods.Count;
            var brandedFoods = await usdaFoodSearchService.SearchBrandedAsync(search, remaining, cancellationToken);
            var existingIds = foods.Select(m => m.FdcId).ToHashSet();
            foods.AddRange(brandedFoods.Where(f => !existingIds.Contains(f.FdcId)));
        }

        return foods.Select(ToSuggestion).ToList();
    }

    private ProductSearchSuggestionModel ToSuggestion(UsdaFoodModel food) =>
        new(
            Source,
            food.Description,
            null,
            food.FoodCategory,
            null,
            food.FdcId,
            null,
            null,
            null,
            null,
            null,
            null);
}
