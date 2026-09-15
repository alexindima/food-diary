using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;

namespace FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;

public interface IRecipeCommentReadRepository {
    Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default);
}
