using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeLikes.Common;

public interface IRecipeLikeWriteRepository {
    Task<RecipeLike?> GetByUserAndRecipeAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken = default);

    Task<RecipeLike> AddAsync(RecipeLike like, CancellationToken cancellationToken = default);

    Task DeleteAsync(RecipeLike like, CancellationToken cancellationToken = default);

    Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken cancellationToken = default);
}
