using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeReadModelRepository {
    Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
