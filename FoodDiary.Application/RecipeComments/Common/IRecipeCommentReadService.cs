using FoodDiary.Application.Common.Models;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Common;

public interface IRecipeCommentReadService {
    Task<PagedResponse<RecipeCommentModel>> GetPagedByRecipeAsync(
        RecipeId recipeId,
        UserId currentUserId,
        int page,
        int limit,
        CancellationToken cancellationToken);
}
