using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

public sealed class SearchUsdaFoodsQueryHandler(
    IUsdaFoodReadRepository repository,
    IUsdaFoodSearchService brandedSearchService)
    : IQueryHandler<SearchUsdaFoodsQuery, Result<IReadOnlyList<UsdaFoodModel>>> {
    public async Task<Result<IReadOnlyList<UsdaFoodModel>>> Handle(
        SearchUsdaFoodsQuery query,
        CancellationToken cancellationToken) {
        // Search local SR Legacy database first
        IReadOnlyList<UsdaFood> localFoods = await repository.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false);

        var models = localFoods
            .Select(f => new UsdaFoodModel(f.FdcId, f.Description, f.FoodCategory))
            .ToList();

        // Supplement with branded foods from USDA API if local results are sparse
        if (models.Count >= query.Limit) {
            return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
        }

        int remaining = query.Limit - models.Count;
        IReadOnlyList<UsdaFoodModel> brandedFoods = await brandedSearchService.SearchBrandedAsync(
            query.Search, remaining, cancellationToken).ConfigureAwait(false);

        var existingIds = models.Select(m => m.FdcId).ToHashSet();
        IEnumerable<UsdaFoodModel> newBranded = brandedFoods.Where(f => !existingIds.Contains(f.FdcId));
        models.AddRange(newBranded);
        return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
    }
}
