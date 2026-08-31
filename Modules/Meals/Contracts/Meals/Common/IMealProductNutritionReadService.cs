using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Common;

public interface IMealProductNutritionReadService {
    Task<IReadOnlyList<MealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        int limit,
        CancellationToken cancellationToken);
}
