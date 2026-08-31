using FoodDiary.Application.RecipeCommunity.RecipeLikes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeCommunity.RecipeLikes.Common;

public interface IRecipeLikeReadService {
    Task<RecipeLikeStatusModel> GetStatusAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken);
}
