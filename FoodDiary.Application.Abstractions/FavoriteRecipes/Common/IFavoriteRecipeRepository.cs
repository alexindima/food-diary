using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteRecipes.Common;

public interface IFavoriteRecipeRepository {
    Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);

    Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default);

    Task<FavoriteRecipe?> GetByIdAsync(
        FavoriteRecipeId id,
        UserId userId,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<FavoriteRecipe?> GetByRecipeIdAsync(
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
