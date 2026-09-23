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
        UserId userId, CancellationToken cancellationToken = default) =>
        await Project(userId, BuildFavoritesQuery(userId, search: null).AsNoTracking().OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id)
            .Take(PaginationPolicy.MaxCollectionSize)).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<(IReadOnlyList<FavoriteRecipeReadModel> Items, int Total)> GetPageReadModelsAsync(
        UserId userId, int page, int limit, string? search, CancellationToken cancellationToken = default) {
        IQueryable<FavoriteRecipe> query = BuildFavoritesQuery(userId, search);
        int total = await query.AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        if (limit == 0) { return ([], total); }
        List<FavoriteRecipeReadModel> items = await Project(userId, query.AsNoTracking().OrderByDescending(row => row.CreatedAtUtc).ThenByDescending(row => row.Id)
            .Skip((page - 1) * limit).Take(limit)).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (items, total);
    }

    public async Task<IReadOnlyList<FavoriteRecipeReadModel>> GetByRecipeIdsReadModelsAsync(UserId userId,
        IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken = default) {
        if (recipeIds.Count == 0) { return []; }
        RecipeId[] ids = [.. recipeIds];
        return await Project(userId, BuildFavoritesQuery(userId, search: null).AsNoTracking().Where(row => Enumerable.Contains(ids, row.RecipeId)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<FavoriteRecipe> BuildFavoritesQuery(UserId userId, string? search) {
        var query = context.FavoriteRecipes.AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .Join(context.Recipes.AsNoTracking().Where(source => source.UserId == userId || source.Visibility == Visibility.Public),
                favorite => favorite.RecipeId, source => source.Id, (favorite, source) => new { Favorite = favorite, Source = source });
        if (!string.IsNullOrWhiteSpace(search)) {
            string term = "%" + search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            query = query.Where(row => (row.Favorite.Name != null && EF.Functions.ILike(row.Favorite.Name, term, "\\")) ||
                EF.Functions.ILike(row.Source.Name, term, "\\") || row.Source.Steps.SelectMany(step => step.Ingredients).Any(ingredient =>
                    context.Products.AsNoTracking().Any(product => product.Id == ingredient.ProductId && (product.UserId == userId || product.Visibility == Visibility.Public) && EF.Functions.ILike(product.Name, term, "\\")) ||
                    context.Recipes.AsNoTracking().Any(recipe => recipe.Id == ingredient.NestedRecipeId && (recipe.UserId == userId || recipe.Visibility == Visibility.Public) && EF.Functions.ILike(recipe.Name, term, "\\"))));
        }
        return query.AsNoTracking().Select(row => row.Favorite);
    }

    private IQueryable<FavoriteRecipeReadModel> Project(UserId userId, IQueryable<FavoriteRecipe> favorites) =>
        favorites.AsNoTracking().Join(context.Recipes.AsNoTracking(), favorite => favorite.RecipeId, source => source.Id,
            (favorite, source) => new { Favorite = favorite, Source = source })
        .Select(row => new FavoriteRecipeReadModel(
            row.Favorite.Id.Value, row.Favorite.RecipeId.Value, row.Favorite.Name, row.Favorite.CreatedAtUtc,
            row.Source.Name, row.Source.ImageUrl ?? row.Source.Steps.Where(step => step.ImageUrl != null && step.ImageUrl != "").OrderBy(step => step.StepNumber).Select(step => step.ImageUrl).FirstOrDefault(),
            row.Source.TotalCalories ?? row.Source.ManualCalories, row.Source.Servings, row.Source.PrepTime, row.Source.CookTime,
            row.Source.Steps.Sum(step => step.Ingredients.Count)) {
            TotalProteins = row.Source.ManualProteins ?? row.Source.TotalProteins ?? 0,
            TotalFats = row.Source.ManualFats ?? row.Source.TotalFats ?? 0,
            TotalCarbs = row.Source.ManualCarbs ?? row.Source.TotalCarbs ?? 0,
            TotalFiber = row.Source.ManualFiber ?? row.Source.TotalFiber ?? 0,
            IngredientNames = row.Source.Steps.OrderBy(step => step.StepNumber).SelectMany(step => step.Ingredients)
                    .Select(ingredient => context.Products.AsNoTracking().Where(product => product.Id == ingredient.ProductId && (product.UserId == userId || product.Visibility == Visibility.Public)).Select(product => product.Name).FirstOrDefault()
                        ?? context.Recipes.AsNoTracking().Where(recipe => recipe.Id == ingredient.NestedRecipeId && (recipe.UserId == userId || recipe.Visibility == Visibility.Public)).Select(recipe => recipe.Name).FirstOrDefault() ?? "")
                    .Where(name => name != "").Take(10).ToList(),
        });
}
