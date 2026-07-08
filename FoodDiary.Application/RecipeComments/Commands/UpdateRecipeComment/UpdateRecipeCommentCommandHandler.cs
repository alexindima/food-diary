using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;

public sealed class UpdateRecipeCommentCommandHandler(
    IRecipeCommentWriteRepository commentRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateRecipeCommentCommand, Result<RecipeCommentModel>> {
    public async Task<Result<RecipeCommentModel>> Handle(
        UpdateRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeCommentModel>(userIdResult);
        }

        var commentId = (RecipeCommentId)command.CommentId;
        RecipeComment? comment = await commentRepository.GetByIdAsync(commentId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (comment is null) {
            return Result.Failure<RecipeCommentModel>(Errors.RecipeComment.NotFound(command.CommentId));
        }

        if (comment.UserId != userIdResult.Value) {
            return Result.Failure<RecipeCommentModel>(Errors.RecipeComment.NotAuthor);
        }

        comment.UpdateText(command.Text);
        await commentRepository.UpdateAsync(comment, cancellationToken).ConfigureAwait(false);

        return Result.Success(new RecipeCommentModel(
            comment.Id.Value,
            comment.RecipeId.Value,
            comment.UserId.Value,
            AuthorUsername: null,
            AuthorFirstName: null,
            comment.Text,
            comment.CreatedOnUtc,
            comment.ModifiedOnUtc,
            IsOwnedByCurrentUser: true));
    }
}
