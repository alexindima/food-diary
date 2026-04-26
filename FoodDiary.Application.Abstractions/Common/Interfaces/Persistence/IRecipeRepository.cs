using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

public interface IRecipeRepository {
    Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Recipe?> GetByIdAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = true,
        bool includeSteps = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
        int page,
        int limit,
        string? search,
        string? category,
        int? maxPrepTime,
        string sortBy,
        CancellationToken cancellationToken = default);
}
