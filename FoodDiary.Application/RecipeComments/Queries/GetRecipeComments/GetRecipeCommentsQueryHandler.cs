using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;

public class GetRecipeCommentsQueryHandler(IRecipeCommentReadRepository commentRepository)
    : IQueryHandler<GetRecipeCommentsQuery, Result<PagedResponse<RecipeCommentModel>>> {
    public async Task<Result<PagedResponse<RecipeCommentModel>>> Handle(
        GetRecipeCommentsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<PagedResponse<RecipeCommentModel>>(userIdResult.Error);
        }

        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);
        var recipeId = (RecipeId)query.RecipeId;

        (IReadOnlyList<Domain.Entities.Recipes.RecipeComment> items, int total) = await commentRepository.GetPagedByRecipeAsync(
            recipeId, pageNumber, pageSize, cancellationToken).ConfigureAwait(false);

        var models = items.Select(c => new RecipeCommentModel(
            c.Id.Value,
            c.RecipeId.Value,
            c.UserId.Value,
            c.User?.Username,
            c.User?.FirstName,
            c.Text,
            c.CreatedOnUtc,
            c.ModifiedOnUtc,
            c.UserId == userIdResult.Value)).ToList();

        int totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Result.Success(new PagedResponse<RecipeCommentModel>(models, pageNumber, pageSize, totalPages, total));
    }
}
