using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealProductNutritionReadRepository {
    Task<IReadOnlyList<MealProductNutritionReadModel>> GetProductNutritionReadModelsAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
