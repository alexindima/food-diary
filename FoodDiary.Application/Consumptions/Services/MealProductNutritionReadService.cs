using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

public sealed class MealProductNutritionReadService(IMealProductNutritionReadRepository repository)
    : IMealProductNutritionReadService {
    public Task<IReadOnlyList<MealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) =>
        repository.GetProductNutritionReadModelsAsync(userId, date, cancellationToken);
}
