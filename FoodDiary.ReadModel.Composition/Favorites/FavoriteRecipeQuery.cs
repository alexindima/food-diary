using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Favorites;

public sealed class FavoriteRecipeQuery(ICompositionReadContext context) : IFavoriteRecipeQuery {
    public async Task<IReadOnlyList<FavoriteRecipeId>> GetAccessibleIdsAsync(
        UserId userId, FavoriteRecipeId? favoriteId = null,
        IReadOnlyCollection<RecipeId>? sourceIds = null, CancellationToken cancellationToken = default) {
        IQueryable<FavoriteRecipe> query = context.FavoriteRecipes.AsNoTracking().Where(f => f.UserId == userId &&
            context.Recipes.AsNoTracking().Any(source => source.Id == f.RecipeId &&
                (source.UserId == userId || source.Visibility == Visibility.Public)));
        if (favoriteId.HasValue) { query = query.AsNoTracking().Where(f => f.Id == favoriteId.Value); }
        if (sourceIds is not null) { query = query.AsNoTracking().Where(f => sourceIds.Contains(f.RecipeId)); }
        return await query.AsNoTracking().Select(f => f.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
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

}
