using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Common;

public interface IFavoriteRecipeSourceReadService {
    Task<Result<FavoriteRecipeSourceModel>> GetAccessibleAsync(RecipeId id, UserId userId, CancellationToken cancellationToken = default);
}
