using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;

public sealed class GetRecipeCommentsQueryHandler(
    IRecipeCommentReadService commentReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipeCommentsQuery, Result<PagedResponse<RecipeCommentModel>>> {
    public async Task<Result<PagedResponse<RecipeCommentModel>>> Handle(
        GetRecipeCommentsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<RecipeCommentModel>>(userIdResult);
        }

        int pageNumber = Math.Max(query.Page, 1);
        int pageSize = Math.Max(query.Limit, 1);
        var recipeId = (RecipeId)query.RecipeId;

        PagedResponse<RecipeCommentModel> comments = await commentReadService
            .GetPagedByRecipeAsync(recipeId, userIdResult.Value, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(comments);
    }
}
