using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.FavoriteRecipes;

public sealed class FavoriteRecipeRepository(FoodDiaryDbContext context) : IFavoriteRecipeRepository {
    public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
        context.FavoriteRecipes.Add(favorite);
        return Task.FromResult(favorite);
    }

    public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
        context.FavoriteRecipes.Remove(favorite);
        return Task.CompletedTask;
    }

    public async Task<FavoriteRecipe?> GetByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<FavoriteRecipe> query = context.FavoriteRecipes
            .AsSplitQuery()
            .Include(f => f.Recipe)
            .ThenInclude(r => r.Steps)
            .ThenInclude(s => s.Ingredients)
            .AsQueryable();

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteRecipe?> GetByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.RecipeId == recipeId && f.UserId == userId,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(f => f.Recipe)
            .ThenInclude(r => r.Steps)
            .ThenInclude(s => s.Ingredients)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(
        UserId userId,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default) {
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, FavoriteRecipe>();
        }

        List<FavoriteRecipe> favorites = await context.FavoriteRecipes
            .AsNoTracking()
            .Where(f => f.UserId == userId && recipeIds.Contains(f.RecipeId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(f => f.RecipeId);
    }
}
