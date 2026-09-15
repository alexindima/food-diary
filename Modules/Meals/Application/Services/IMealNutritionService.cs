using FoodDiary.Results;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Services;

public interface IMealNutritionService {
    Task<Result<MealNutritionSummary>> CalculateAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken = default);
}
