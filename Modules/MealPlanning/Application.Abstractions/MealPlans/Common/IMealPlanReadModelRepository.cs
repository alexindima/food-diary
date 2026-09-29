using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanReadModelRepository {
    async Task<(IReadOnlyList<MealPlanSummaryReadModel> Items, int Total)> GetPageSummaryReadModelsAsync(
        UserId userId, DietType? dietType, int page, int limit, CancellationToken cancellationToken = default) {
        IReadOnlyList<MealPlanSummaryReadModel> curated = await GetCuratedSummaryReadModelsAsync(dietType, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MealPlanSummaryReadModel> owned = await GetByUserSummaryReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        MealPlanSummaryReadModel[] all = [.. curated, .. owned];
        return (all.Skip((page - 1) * limit).Take(limit).ToArray(), all.Length);
    }

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
