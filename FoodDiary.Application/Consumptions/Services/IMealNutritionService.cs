using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

public interface IMealNutritionService {
    Task<Result<MealNutritionSummary>> CalculateAsync(
        Meal meal,
        UserId userId,
        CancellationToken cancellationToken = default);
}
