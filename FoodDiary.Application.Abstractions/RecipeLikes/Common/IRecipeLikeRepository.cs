using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecipeLikes.Common;

public interface IRecipeLikeRepository {
    Task<RecipeLike?> GetByUserAndRecipeAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken = default);

    Task<RecipeLike> AddAsync(RecipeLike like, CancellationToken cancellationToken = default);

    Task DeleteAsync(RecipeLike like, CancellationToken cancellationToken = default);

    Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken cancellationToken = default);
}
