using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

public sealed class MealProductNutritionReadService(IMealProductNutritionReadRepository repository)
    : IMealProductNutritionReadService {
    public Task<IReadOnlyList<MealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken) =>
        repository.GetProductNutritionReadModelsAsync(userId, date, limit, cancellationToken);
}
