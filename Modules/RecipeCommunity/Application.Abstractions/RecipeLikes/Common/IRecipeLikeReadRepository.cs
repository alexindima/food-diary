using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeLikes.Common;

public interface IRecipeLikeReadRepository {
    Task<RecipeLike?> GetByUserAndRecipeAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken = default);

    async Task<bool> ExistsByUserAndRecipeAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken = default) =>
        await GetByUserAndRecipeAsync(userId, recipeId, cancellationToken).ConfigureAwait(false) is not null;

    Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken cancellationToken = default);
}
