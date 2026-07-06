using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeWriteRepository {
    Task<FavoriteRecipe?> GetByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipe?> GetByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);
}
