using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

public class SearchUsdaFoodsQueryHandler(
    IUsdaFoodRepository repository,
    IUsdaFoodSearchService brandedSearchService)
    : IQueryHandler<SearchUsdaFoodsQuery, Result<IReadOnlyList<UsdaFoodModel>>> {
    public async Task<Result<IReadOnlyList<UsdaFoodModel>>> Handle(
        SearchUsdaFoodsQuery query,
        CancellationToken cancellationToken) {
        // Search local SR Legacy database first
        var localFoods = await repository.SearchAsync(query.Search, query.Limit, cancellationToken);

        var models = localFoods
            .Select(f => new UsdaFoodModel(f.FdcId, f.Description, f.FoodCategory))
            .ToList();

        // Supplement with branded foods from USDA API if local results are sparse
        if (models.Count < query.Limit) {
            var remaining = query.Limit - models.Count;
            var brandedFoods = await brandedSearchService.SearchBrandedAsync(
                query.Search, remaining, cancellationToken);

            var existingIds = models.Select(m => m.FdcId).ToHashSet();
            var newBranded = brandedFoods.Where(f => !existingIds.Contains(f.FdcId));
            models.AddRange(newBranded);
        }

        return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
    }
}
