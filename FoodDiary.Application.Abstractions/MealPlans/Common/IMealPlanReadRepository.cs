using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.MealPlans.Common;

public interface IMealPlanReadRepository {
    Task<MealPlan?> GetByIdAsync(
        MealPlanId id,
        bool includeDays = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealPlan>> GetCuratedAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<MealPlanSummaryReadModel>> GetCuratedSummaryReadModelsAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<MealPlan> plans = await GetCuratedAsync(dietType, cancellationToken).ConfigureAwait(false);
        return [.. plans.Select(ToSummaryReadModel)];
    }

    Task<IReadOnlyList<MealPlan>> GetByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<MealPlanSummaryReadModel>> GetByUserSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<MealPlan> plans = await GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. plans.Select(ToSummaryReadModel)];
    }

    async Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id,
        CancellationToken cancellationToken = default) {
        MealPlan? plan = await GetByIdAsync(id, includeDays: true, cancellationToken).ConfigureAwait(false);
        return plan is null ? null : ToReadModel(plan);
    }

    private static MealPlanSummaryReadModel ToSummaryReadModel(MealPlan plan) =>
        new(
            plan.Id.Value,
            plan.Name,
            plan.Description,
            plan.DietType.ToString(),
            plan.DurationDays,
            plan.TargetCaloriesPerDay,
            plan.IsCurated,
            plan.Days.SelectMany(static day => day.Meals).Select(static meal => meal.RecipeId).Distinct().Count());

    private static MealPlanReadModel ToReadModel(MealPlan plan) =>
        new(
            plan.Id.Value,
            plan.UserId?.Value,
            plan.Name,
            plan.Description,
            plan.DietType.ToString(),
            plan.DurationDays,
            plan.TargetCaloriesPerDay,
            plan.IsCurated,
            [.. plan.Days
                .OrderBy(static day => day.DayNumber)
                .Select(static day => new MealPlanDayReadModel(
                    day.Id.Value,
                    day.DayNumber,
                    [.. day.Meals
                        .OrderBy(static meal => meal.MealType)
                        .Select(static meal => new MealPlanMealReadModel(
                            meal.Id.Value,
                            meal.MealType.ToString(),
                            meal.RecipeId.Value,
                            meal.Recipe?.Name,
                            meal.Servings,
                            meal.Recipe is { Servings: > 0 } ? meal.Recipe.Servings : 1,
                            meal.Recipe?.TotalCalories,
                            meal.Recipe?.TotalProteins,
                            meal.Recipe?.TotalFats,
                            meal.Recipe?.TotalCarbs))]))]);
}
