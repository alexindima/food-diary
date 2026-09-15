using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Usda.Contracts.Common;

public interface IUsdaMealNutritionReadService {
    Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken);
}
