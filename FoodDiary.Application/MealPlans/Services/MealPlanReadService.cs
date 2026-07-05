using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Services;

public sealed class MealPlanReadService(IMealPlanReadRepository mealPlanRepository)
    : IMealPlanReadService {
    public async Task<IReadOnlyList<MealPlanSummaryModel>> GetAllAsync(
        UserId userId,
        DietType? dietTypeFilter,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealPlan> curatedPlans = await mealPlanRepository
            .GetCuratedAsync(dietTypeFilter, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<MealPlan> userPlans = await mealPlanRepository
            .GetByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return curatedPlans
            .Concat(userPlans)
            .Select(plan => plan.ToSummaryModel())
            .ToList();
    }

    public async Task<MealPlanModel?> GetAccessibleByIdAsync(
        MealPlanId mealPlanId,
        UserId userId,
        CancellationToken cancellationToken) {
        MealPlan? plan = await mealPlanRepository
            .GetByIdAsync(mealPlanId, includeDays: true, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null || (!plan.IsCurated && plan.UserId != userId)) {
            return null;
        }

        return plan.ToModel();
    }
}
