using FoodDiary.Results;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

public interface IMealNutritionService {
    Task<Result<MealNutritionSummary>> CalculateAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken = default);
}
