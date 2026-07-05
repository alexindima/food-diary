using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteRecipes.Services;

public sealed class FavoriteRecipeReadService(IFavoriteRecipeReadRepository favoriteRecipeRepository)
    : IFavoriteRecipeReadService {
    public async Task<IReadOnlyList<FavoriteRecipeModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipe> favorites = await favoriteRecipeRepository.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

    public Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        favoriteRecipeRepository.ExistsByRecipeIdAsync(recipeId, userId, cancellationToken);
}
