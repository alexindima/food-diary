using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Usda.Common;

namespace FoodDiary.Application.Usda.Queries.SearchUsdaFoods;

public sealed class SearchUsdaFoodsQueryHandler(IUsdaFoodReadService readService)
    : IQueryHandler<SearchUsdaFoodsQuery, Result<IReadOnlyList<UsdaFoodModel>>> {
    public async Task<Result<IReadOnlyList<UsdaFoodModel>>> Handle(
        SearchUsdaFoodsQuery query,
        CancellationToken cancellationToken) {
        return await readService.SearchAsync(query.Search, query.Limit, cancellationToken).ConfigureAwait(false);
    }
}
