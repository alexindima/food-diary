using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeQuery {
    Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteRecipeId>> GetAccessibleIdsAsync(
        UserId userId,
        FavoriteRecipeId? favoriteId = null,
        IReadOnlyCollection<RecipeId>? sourceIds = null,
        CancellationToken cancellationToken = default);
}
