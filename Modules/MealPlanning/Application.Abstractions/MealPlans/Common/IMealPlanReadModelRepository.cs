using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Enums;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;

public interface IMealPlanReadModelRepository {
    async Task<(IReadOnlyList<MealPlanSummaryReadModel> Items, int Total)> GetPageSummaryReadModelsAsync(
        UserId userId, DietType? dietType, int page, int limit, bool? isCurated = null, CancellationToken cancellationToken = default) {
        IReadOnlyList<MealPlanSummaryReadModel> curated = isCurated is false ? [] : await GetCuratedSummaryReadModelsAsync(dietType, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MealPlanSummaryReadModel> owned = isCurated is true ? [] : await GetByUserSummaryReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        MealPlanSummaryReadModel[] all = [.. curated.Concat(owned)
            .Where(plan => (!dietType.HasValue || string.Equals(plan.DietType, dietType.Value.ToString(), StringComparison.OrdinalIgnoreCase)) &&
                (!isCurated.HasValue || plan.IsCurated == isCurated.Value))];
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
