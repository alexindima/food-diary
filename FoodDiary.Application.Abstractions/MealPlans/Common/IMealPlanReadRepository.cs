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

    Task<IReadOnlyList<MealPlanSummaryReadModel>> GetCuratedSummaryReadModelsAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealPlan>> GetByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealPlanSummaryReadModel>> GetByUserSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id,
        CancellationToken cancellationToken = default);
}
