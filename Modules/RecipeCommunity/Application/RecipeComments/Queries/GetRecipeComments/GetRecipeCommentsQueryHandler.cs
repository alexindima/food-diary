using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Queries.GetRecipeComments;

public sealed class GetRecipeCommentsQueryHandler(
    IRecipeCommentReadModelRepository commentRepository,
    IRecipeAccessService recipeAccessService,
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
        RecipeOverviewReadItem? recipe = await recipeAccessService.GetAccessibleByIdAsync(
            recipeId,
            userIdResult.Value,
            includePublic: true,
            cancellationToken).ConfigureAwait(false);
        if (recipe is null) {
            return Result.Failure<PagedResponse<RecipeCommentModel>>(RecipeErrors.NotFound(query.RecipeId));
        }

        PagedResponse<RecipeCommentModel> comments = await GetPagedByRecipeAsync(recipeId, userIdResult.Value, pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(comments);
    }
    private async Task<PagedResponse<RecipeCommentModel>> GetPagedByRecipeAsync(
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
