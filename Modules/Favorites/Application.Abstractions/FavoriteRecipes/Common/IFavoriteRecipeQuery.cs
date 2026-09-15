using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeQuery {
    Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteRecipeId>> GetAccessibleIdsAsync(
        UserId userId,
        FavoriteRecipeId? favoriteId = null,
        IReadOnlyCollection<RecipeId>? sourceIds = null,
        CancellationToken cancellationToken = default);
}
