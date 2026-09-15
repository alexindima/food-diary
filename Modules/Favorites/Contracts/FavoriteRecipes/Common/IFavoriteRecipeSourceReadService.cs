using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Common;

public interface IFavoriteRecipeSourceReadService {
    Task<Result<FavoriteRecipeSourceModel>> GetAccessibleAsync(RecipeId id, UserId userId, CancellationToken cancellationToken = default);
}
