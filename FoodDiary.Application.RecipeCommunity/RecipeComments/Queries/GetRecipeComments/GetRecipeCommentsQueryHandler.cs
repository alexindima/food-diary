using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Common;
using FoodDiary.Application.RecipeCommunity.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeCommunity.RecipeComments.Queries.GetRecipeComments;

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

        int pageSize = PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1);
        int pageNumber = PaginationPolicy.NormalizePage(query.Page);
        var recipeId = (RecipeId)query.RecipeId;

        PagedResponse<RecipeCommentModel> comments = await commentReadService
            .GetPagedByRecipeAsync(recipeId, userIdResult.Value, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(comments);
    }
}
