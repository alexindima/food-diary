using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeLikes.Common;

public interface IRecipeLikeReadService {
    Task<RecipeLikeStatusModel> GetStatusAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken);
}
