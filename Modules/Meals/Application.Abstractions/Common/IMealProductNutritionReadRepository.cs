using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public interface IMealProductNutritionReadRepository {
    Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken = default);
}
