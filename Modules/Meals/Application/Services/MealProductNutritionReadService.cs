using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

public sealed class MealProductNutritionReadService(IMealProductNutritionReadRepository repository)
    : IUsdaMealNutritionReadService {
    public Task<IReadOnlyList<UsdaMealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken) =>
        repository.GetProductNutritionReadModelsAsync(userId, date, limit, cancellationToken);
}
