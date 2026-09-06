using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteRecipes;

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
        IQueryable<FavoriteRecipe> query = context.FavoriteRecipes.Where(
            favorite => favorite.Id == id && favorite.UserId == userId &&
                context.Recipes.AsNoTracking().Any(source => source.Id == favorite.RecipeId &&
                    (source.UserId == userId || source.Visibility == Visibility.Public)));

        // EF tracking applies to the whole query, including nested access checks.
        query = asTracking ? query.AsTracking() : query.AsNoTracking();
        return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteRecipe?> GetOwnedByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<FavoriteRecipe> query = context.FavoriteRecipes;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            favorite => favorite.Id == id && favorite.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<FavoriteRecipe?> GetByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.RecipeId == recipeId && f.UserId == userId &&
                    context.Recipes.AsNoTracking().Any(source => source.Id == f.RecipeId && (source.UserId == userId || source.Visibility == Visibility.Public)),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .AnyAsync(
                f => f.RecipeId == recipeId && f.UserId == userId &&
                    context.Recipes.AsNoTracking().Any(source => source.Id == f.RecipeId && (source.UserId == userId || source.Visibility == Visibility.Public)),
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()

            .Where(f => f.UserId == userId &&
                context.Recipes.AsNoTracking().Any(source => source.Id == f.RecipeId && (source.UserId == userId || source.Visibility == Visibility.Public)))
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.FavoriteRecipes
            .AsNoTracking()
            .Where(f => f.UserId == userId &&
                context.Recipes.AsNoTracking().Any(source => source.Id == f.RecipeId && (source.UserId == userId || source.Visibility == Visibility.Public)))
            .OrderByDescending(f => f.CreatedAtUtc)
            .Take(PaginationPolicy.MaxCollectionSize)
            .Join(context.Recipes.AsNoTracking(), favorite => favorite.RecipeId, source => source.Id, (favorite, source) => new { Favorite = favorite, Source = source })
            .Select(row => new FavoriteRecipeReadModel(
                row.Favorite.Id.Value,
                row.Favorite.RecipeId.Value,
                row.Favorite.Name,
                row.Favorite.CreatedAtUtc,
                row.Source.Name,
                row.Source.ImageUrl,
                row.Source.TotalCalories ?? row.Source.ManualCalories,
                row.Source.Servings,
                row.Source.PrepTime,
                row.Source.CookTime,
                row.Source.Steps.Sum(step => step.Ingredients.Count)))
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
            .Where(f => f.UserId == userId && recipeIds.Contains(f.RecipeId) &&
                context.Recipes.AsNoTracking().Any(source => source.Id == f.RecipeId && (source.UserId == userId || source.Visibility == Visibility.Public)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return favorites.ToDictionary(f => f.RecipeId);
    }
}
