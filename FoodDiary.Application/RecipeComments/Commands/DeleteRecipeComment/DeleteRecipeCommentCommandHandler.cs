using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;

public class DeleteRecipeCommentCommandHandler(
    IRecipeCommentRepository commentRepository,
    IRecipeRepository recipeRepository)
    : ICommandHandler<DeleteRecipeCommentCommand, Result> {
    public async Task<Result> Handle(
        DeleteRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var commentId = (RecipeCommentId)command.CommentId;
        var comment = await commentRepository.GetByIdAsync(commentId, asTracking: true, cancellationToken);

        if (comment is null) {
            return Result.Failure(Errors.RecipeComment.NotFound(command.CommentId));
        }

        // Author or recipe owner can delete
        var isAuthor = comment.UserId == userIdResult.Value;
        if (!isAuthor) {
            var recipeId = (RecipeId)command.RecipeId;
            var recipe = await recipeRepository.GetByIdAsync(
                recipeId, userIdResult.Value, includePublic: false, cancellationToken: cancellationToken);

            if (recipe is null) {
                return Result.Failure(Errors.RecipeComment.NotAuthor);
            }
        }

        await commentRepository.DeleteAsync(comment, cancellationToken);
        return Result.Success();
    }
}
