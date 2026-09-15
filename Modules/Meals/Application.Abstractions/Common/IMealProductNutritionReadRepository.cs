using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public interface IMealProductNutritionReadRepository {
    Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken = default);
}
