using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Common;

public interface IMealPlanReadService {
    Task<IReadOnlyList<MealPlanSummaryModel>> GetAllAsync(
        UserId userId,
        DietType? dietTypeFilter,
        CancellationToken cancellationToken);

    Task<MealPlanModel?> GetAccessibleByIdAsync(
        MealPlanId mealPlanId,
        UserId userId,
        CancellationToken cancellationToken);
}
