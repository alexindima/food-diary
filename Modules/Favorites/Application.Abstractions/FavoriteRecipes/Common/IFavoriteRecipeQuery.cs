using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeQuery {
    Task<IReadOnlyList<FavoriteRecipeReadModel>> GetByRecipeIdsReadModelsAsync(UserId userId, IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FavoriteRecipeReadModel> Items, int Total)> GetPageReadModelsAsync(
        UserId userId, int page, int limit, string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteRecipeReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FavoriteRecipeId>> GetAccessibleIdsAsync(
        UserId userId,
        FavoriteRecipeId? favoriteId = null,
        IReadOnlyCollection<RecipeId>? sourceIds = null,
        CancellationToken cancellationToken = default);
}
