using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Commands.UpdateRecipeComment;

public class UpdateRecipeCommentCommandHandler(IRecipeCommentRepository commentRepository)
    : ICommandHandler<UpdateRecipeCommentCommand, Result<RecipeCommentModel>> {
    public async Task<Result<RecipeCommentModel>> Handle(
        UpdateRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeCommentModel>(userIdResult.Error);
        }

        var commentId = (RecipeCommentId)command.CommentId;
        var comment = await commentRepository.GetByIdAsync(commentId, asTracking: true, cancellationToken);

        if (comment is null) {
            return Result.Failure<RecipeCommentModel>(Errors.RecipeComment.NotFound(command.CommentId));
        }

        if (comment.UserId != userIdResult.Value) {
            return Result.Failure<RecipeCommentModel>(Errors.RecipeComment.NotAuthor);
        }

        comment.UpdateText(command.Text);
        await commentRepository.UpdateAsync(comment, cancellationToken);

        return Result.Success(new RecipeCommentModel(
            comment.Id.Value,
            comment.RecipeId.Value,
            comment.UserId.Value,
            null,
            null,
            comment.Text,
            comment.CreatedOnUtc,
            comment.ModifiedOnUtc,
            true));
    }
}
