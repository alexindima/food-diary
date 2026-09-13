using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteRecipes;

public sealed class FavoriteRecipeRepository(DbSet<FavoriteRecipe> favorites, IFavoriteRecipeQuery queries) : IFavoriteRecipeRepository {
    public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
        favorites.Add(favorite);
        return Task.FromResult(favorite);
    }

    public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
        favorites.Remove(favorite);
        return Task.CompletedTask;
    }

    public async Task<FavoriteRecipe?> GetByIdAsync(FavoriteRecipeId id, UserId userId,
        bool asTracking = false, CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipeId> ids = await queries.GetAccessibleIdsAsync(userId, favoriteId: id, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (ids.Count == 0) { return null; }
        return await GetOwnedByIdAsync(id, userId, asTracking, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteRecipe?> GetOwnedByIdAsync(FavoriteRecipeId id, UserId userId,
        bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<FavoriteRecipe> query = asTracking ? favorites.AsTracking() : favorites.AsNoTracking();
        return await query.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteRecipe?> GetByRecipeIdAsync(RecipeId recipeId, UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipeId> ids = await queries.GetAccessibleIdsAsync(userId, sourceIds: [recipeId], cancellationToken: cancellationToken).ConfigureAwait(false);
        return await favorites.AsNoTracking().FirstOrDefaultAsync(f => f.UserId == userId && ids.Contains(f.Id), cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByRecipeIdAsync(RecipeId recipeId, UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipeId> ids = await queries.GetAccessibleIdsAsync(userId, sourceIds: [recipeId], cancellationToken: cancellationToken).ConfigureAwait(false);
        return ids.Count != 0;
    }

    public async Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipeId> ids = await queries.GetAccessibleIdsAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await favorites.AsNoTracking().Where(f => f.UserId == userId && ids.Contains(f.Id))
            .OrderByDescending(f => f.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
        queries.GetAllReadModelsAsync(userId, cancellationToken);

    public async Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(UserId userId,
        IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken = default) {
        if (recipeIds.Count == 0) { return new Dictionary<RecipeId, FavoriteRecipe>(); }
        IReadOnlyList<FavoriteRecipeId> ids = await queries.GetAccessibleIdsAsync(userId, sourceIds: recipeIds, cancellationToken: cancellationToken).ConfigureAwait(false);
        List<FavoriteRecipe> results = await favorites.AsNoTracking()
            .Where(f => f.UserId == userId && ids.Contains(f.Id)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.ToDictionary(f => f.RecipeId);
    }
}
