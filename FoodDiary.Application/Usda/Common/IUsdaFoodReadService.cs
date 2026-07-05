using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Usda.Models;

namespace FoodDiary.Application.Usda.Common;

public interface IUsdaFoodReadService {
    Task<Result<IReadOnlyList<UsdaFoodModel>>> SearchAsync(
        string search,
        int limit,
        CancellationToken cancellationToken);

    Task<Result<UsdaFoodDetailModel>> GetDetailAsync(int fdcId, CancellationToken cancellationToken);
}
