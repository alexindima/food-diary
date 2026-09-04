using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeReadService {
    Task<IReadOnlyList<FavoriteRecipeModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
