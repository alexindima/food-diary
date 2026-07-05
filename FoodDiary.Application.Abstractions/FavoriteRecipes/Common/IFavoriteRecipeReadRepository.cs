using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeReadRepository {
    Task<FavoriteRecipe?> GetByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipe?> GetByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        await GetByRecipeIdAsync(recipeId, userId, cancellationToken).ConfigureAwait(false) is not null;

    Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipe> favorites = await GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(static favorite => new FavoriteRecipeReadModel(
            favorite.Id.Value,
            favorite.RecipeId.Value,
            favorite.Name,
            favorite.CreatedAtUtc,
            favorite.Recipe.Name,
            favorite.Recipe.ImageUrl,
            favorite.Recipe.TotalCalories ?? favorite.Recipe.ManualCalories,
            favorite.Recipe.Servings,
            favorite.Recipe.PrepTime,
            favorite.Recipe.CookTime,
            favorite.Recipe.Steps.Sum(step => step.Ingredients.Count)))];
    }

    Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(
        UserId userId,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);
}
