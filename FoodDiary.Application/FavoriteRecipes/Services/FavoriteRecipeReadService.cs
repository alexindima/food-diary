using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.FavoriteRecipes.Common;
using FoodDiary.Application.FavoriteRecipes.Mappings;
using FoodDiary.Application.FavoriteRecipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteRecipes.Services;

public sealed class FavoriteRecipeReadService(
    IFavoriteRecipeReadModelRepository favoriteRecipeReadModelRepository,
    IFavoriteRecipeReadRepository favoriteRecipeRepository)
    : IFavoriteRecipeReadService {
    public async Task<IReadOnlyList<FavoriteRecipeModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<FavoriteRecipeReadModel> favorites = await favoriteRecipeReadModelRepository.GetAllReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        return [.. favorites.Select(favorite => favorite.ToModel())];
    }

    public Task<bool> ExistsByRecipeIdAsync(
        RecipeId recipeId,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        favoriteRecipeRepository.ExistsByRecipeIdAsync(recipeId, userId, cancellationToken);
}
