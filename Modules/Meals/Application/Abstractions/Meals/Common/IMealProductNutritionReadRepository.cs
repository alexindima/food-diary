using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealProductNutritionReadRepository {
    Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken = default);
}
