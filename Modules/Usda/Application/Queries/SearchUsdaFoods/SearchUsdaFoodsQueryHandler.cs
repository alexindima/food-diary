using FoodDiary.Mediator;
using FoodDiary.Modules.Usda.Contracts.Queries.SearchUsdaFoods;
using FoodDiary.Results;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Application.Queries.SearchUsdaFoods;

public sealed class SearchUsdaFoodsQueryHandler(IUsdaFoodReadModelRepository repository, IUsdaFoodSearchService brandedSearchService)
    : IRequestHandler<SearchUsdaFoodsQuery, Result<IReadOnlyList<UsdaFoodModel>>> {
    public async Task<Result<IReadOnlyList<UsdaFoodModel>>> Handle(
        SearchUsdaFoodsQuery request,
        CancellationToken cancellationToken) {
        return await SearchAsync(request.Search, request.Limit, cancellationToken).ConfigureAwait(false);
    }
    private async Task<Result<IReadOnlyList<UsdaFoodModel>>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken) {
        IReadOnlyList<UsdaFoodReadModel> localFoods = await repository.SearchReadModelsAsync(search, limit, cancellationToken).ConfigureAwait(false);

        var models = localFoods
            .Select(static food => new UsdaFoodModel(food.FdcId, food.Description, food.FoodCategory))
            .ToList();

        if (models.Count >= limit) {
            return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
        }

        int remaining = limit - models.Count;
        IReadOnlyList<UsdaFoodModel> brandedFoods = await brandedSearchService.SearchBrandedAsync(
            search, remaining, cancellationToken).ConfigureAwait(false);

        var existingIds = models.Select(static model => model.FdcId).ToHashSet();
        IEnumerable<UsdaFoodModel> newBranded = brandedFoods.Where(food => !existingIds.Contains(food.FdcId));
        models.AddRange(newBranded);
        return Result.Success<IReadOnlyList<UsdaFoodModel>>(models);
    }

}
