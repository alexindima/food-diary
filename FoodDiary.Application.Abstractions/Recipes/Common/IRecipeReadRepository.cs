using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeReadRepository {
    Task<Recipe?> GetByIdAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = true,
        bool includeSteps = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<Recipe?> GetByIdForUpdateAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = false,
        bool includeSteps = false,
        CancellationToken cancellationToken = default) =>
        GetByIdAsync(
            id,
            userId,
            includePublic,
            includeSteps,
            asTracking: true,
            cancellationToken);

    Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task<int> GetUsageCountAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);
}
