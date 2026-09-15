using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;

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

    Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(
        UserId userId,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);
}
