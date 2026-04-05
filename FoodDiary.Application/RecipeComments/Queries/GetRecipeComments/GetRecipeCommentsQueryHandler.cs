using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Queries.GetRecipeComments;

public class GetRecipeCommentsQueryHandler(IRecipeCommentRepository commentRepository)
    : IQueryHandler<GetRecipeCommentsQuery, Result<PagedResponse<RecipeCommentModel>>> {
    public async Task<Result<PagedResponse<RecipeCommentModel>>> Handle(
        GetRecipeCommentsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<PagedResponse<RecipeCommentModel>>(userIdResult.Error);
        }

        var pageNumber = Math.Max(query.Page, 1);
        var pageSize = Math.Max(query.Limit, 1);
        var recipeId = (RecipeId)query.RecipeId;

        var (items, total) = await commentRepository.GetPagedByRecipeAsync(
            recipeId, pageNumber, pageSize, cancellationToken);

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

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return Result.Success(new PagedResponse<RecipeCommentModel>(models, pageNumber, pageSize, totalPages, total));
    }
}
