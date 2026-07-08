using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Usda.Common;

namespace FoodDiary.Application.Usda.Queries.GetMicronutrients;

public sealed class GetMicronutrientsQueryHandler(IUsdaFoodReadService readService)
    : IQueryHandler<GetMicronutrientsQuery, Result<UsdaFoodDetailModel>> {
    public async Task<Result<UsdaFoodDetailModel>> Handle(
        GetMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetDetailAsync(query.FdcId, cancellationToken).ConfigureAwait(false);
    }
}
