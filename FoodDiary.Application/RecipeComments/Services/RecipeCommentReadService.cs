using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Services;

public sealed class RecipeCommentReadService(IRecipeCommentReadRepository commentRepository)
    : IRecipeCommentReadService {
    public async Task<PagedResponse<RecipeCommentModel>> GetPagedByRecipeAsync(
        RecipeId recipeId,
        UserId currentUserId,
        int page,
        int limit,
        CancellationToken cancellationToken) {
        (IReadOnlyList<Domain.Entities.Recipes.RecipeComment> items, int total) = await commentRepository
            .GetPagedByRecipeAsync(recipeId, page, limit, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RecipeCommentModel> models = [
            .. items
            .Select(comment => new RecipeCommentModel(
                comment.Id.Value,
                comment.RecipeId.Value,
                comment.UserId.Value,
                comment.User?.Username,
                comment.User?.FirstName,
                comment.Text,
                comment.CreatedOnUtc,
                comment.ModifiedOnUtc,
                comment.UserId == currentUserId)),
        ];

        int totalPages = (int)Math.Ceiling(total / (double)limit);
        return new PagedResponse<RecipeCommentModel>(models, page, limit, totalPages, total);
    }
}
