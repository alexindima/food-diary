using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaMealNutritionReadService {
    Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken);
}
