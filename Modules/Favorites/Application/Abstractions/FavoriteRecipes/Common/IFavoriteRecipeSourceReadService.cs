using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.FavoriteRecipes.Common;

public interface IFavoriteRecipeSourceReadService {
    Task<Result<FavoriteRecipeSourceModel>> GetAccessibleAsync(RecipeId id, UserId userId, CancellationToken cancellationToken = default);
}
