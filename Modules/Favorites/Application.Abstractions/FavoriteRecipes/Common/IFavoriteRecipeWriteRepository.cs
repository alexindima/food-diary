using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeWriteRepository {
    Task<FavoriteRecipe?> GetByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipe?> GetOwnedByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default) =>
        GetByIdAsync(id, userId, asTracking, cancellationToken);

    Task<FavoriteRecipe?> GetByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);
}
