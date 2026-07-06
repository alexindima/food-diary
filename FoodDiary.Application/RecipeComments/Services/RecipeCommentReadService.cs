using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Services;

public sealed class RecipeCommentReadService(IRecipeCommentReadModelRepository commentRepository)
    : IRecipeCommentReadService {
    public async Task<PagedResponse<RecipeCommentModel>> GetPagedByRecipeAsync(
        RecipeId recipeId,
        UserId currentUserId,
        int page,
        int limit,
        CancellationToken cancellationToken) {
        (IReadOnlyList<RecipeCommentReadModel> items, int total) = await commentRepository
            .GetPagedReadModelsByRecipeAsync(recipeId, page, limit, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RecipeCommentModel> models = [
            .. items
            .Select(comment => new RecipeCommentModel(
                comment.Id,
                comment.RecipeId,
                comment.UserId,
                comment.AuthorUsername,
                comment.AuthorFirstName,
                comment.Text,
                comment.CreatedAtUtc,
                comment.ModifiedAtUtc,
                comment.UserId == currentUserId.Value)),
        ];

        int totalPages = (int)Math.Ceiling(total / (double)limit);
        return new PagedResponse<RecipeCommentModel>(models, page, limit, totalPages, total);
    }
}
