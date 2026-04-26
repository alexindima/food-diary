using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.FavoriteRecipes;

public class FavoriteRecipeRepository(FoodDiaryDbContext context) : IFavoriteRecipeRepository {
    public async Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
        context.FavoriteRecipes.Add(favorite);
        await context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) {
        context.FavoriteRecipes.Remove(favorite);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FavoriteRecipe?> GetByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        var query = context.FavoriteRecipes
            .Include(f => f.Recipe)
            .ThenInclude(r => r.Steps)
            .ThenInclude(s => s.Ingredients)
            .AsQueryable();

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            f => f.Id == id && f.UserId == userId,
            cancellationToken);
    }

    public async Task<FavoriteRecipe?> GetByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.RecipeId == recipeId && f.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .Include(f => f.Recipe)
            .ThenInclude(r => r.Steps)
            .ThenInclude(s => s.Ingredients)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(
        UserId userId,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default) {
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, FavoriteRecipe>();
        }

        var favorites = await context.FavoriteRecipes
            .AsNoTracking()
            .Where(f => f.UserId == userId && recipeIds.Contains(f.RecipeId))
            .ToListAsync(cancellationToken);

        return favorites.ToDictionary(f => f.RecipeId);
    }
}
