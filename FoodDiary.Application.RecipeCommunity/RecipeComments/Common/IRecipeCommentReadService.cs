using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeCommunity.RecipeComments.Common;

public interface IRecipeCommentReadService {
    Task<PagedResponse<RecipeCommentModel>> GetPagedByRecipeAsync(
        RecipeId recipeId,
        UserId currentUserId,
        int page,
        int limit,
        CancellationToken cancellationToken);
}
