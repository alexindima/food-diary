using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Common;

public interface IMealProductNutritionReadService {
    Task<IReadOnlyList<MealProductNutritionReadModel>> GetForDateAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken);
}
