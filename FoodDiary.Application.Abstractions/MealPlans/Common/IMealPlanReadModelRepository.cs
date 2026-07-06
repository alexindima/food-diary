using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.MealPlans.Common;

public interface IMealPlanReadModelRepository {
    Task<IReadOnlyList<MealPlanSummaryReadModel>> GetCuratedSummaryReadModelsAsync(
        DietType? dietType = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MealPlanSummaryReadModel>> GetByUserSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id,
        CancellationToken cancellationToken = default);
}