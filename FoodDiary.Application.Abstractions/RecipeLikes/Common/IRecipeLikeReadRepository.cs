using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecipeLikes.Common;

public interface IRecipeLikeReadRepository {
    Task<RecipeLike?> GetByUserAndRecipeAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken = default);

    Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken cancellationToken = default);
}
