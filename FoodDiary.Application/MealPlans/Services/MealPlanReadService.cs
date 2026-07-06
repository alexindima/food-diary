using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Application.MealPlans.Common;
using FoodDiary.Application.MealPlans.Mappings;
using FoodDiary.Application.MealPlans.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Services;

public sealed class MealPlanReadService(IMealPlanReadModelRepository mealPlanRepository)
    : IMealPlanReadService {
    public async Task<IReadOnlyList<MealPlanSummaryModel>> GetAllAsync(
        UserId userId,
        DietType? dietTypeFilter,
        CancellationToken cancellationToken) {
        IReadOnlyList<MealPlanSummaryReadModel> curatedPlans = await mealPlanRepository
            .GetCuratedSummaryReadModelsAsync(dietTypeFilter, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<MealPlanSummaryReadModel> userPlans = await mealPlanRepository
            .GetByUserSummaryReadModelsAsync(userId, cancellationToken)
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
        MealPlanReadModel? plan = await mealPlanRepository
            .GetReadModelByIdAsync(mealPlanId, cancellationToken)
            .ConfigureAwait(false);

        if (plan is null || (!plan.IsCurated && plan.UserId != userId.Value)) {
            return null;
        }

        return plan.ToModel();
    }
}
