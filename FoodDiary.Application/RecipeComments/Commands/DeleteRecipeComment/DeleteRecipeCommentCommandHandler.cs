using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.RecipeComments.Commands.DeleteRecipeComment;

public sealed class DeleteRecipeCommentCommandHandler(
    IRecipeCommentWriteRepository commentRepository,
    IRecipeAccessService recipeAccessService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteRecipeCommentCommand, Result> {
    public async Task<Result> Handle(
        DeleteRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        var recipeId = (RecipeId)command.RecipeId;
        var commentId = (RecipeCommentId)command.CommentId;
        RecipeComment? comment = await commentRepository.GetByIdAsync(commentId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (comment is null || comment.RecipeId != recipeId) {
            return Result.Failure(Errors.RecipeComment.NotFound(command.CommentId));
        }

        // Author or recipe owner can delete
        bool isAuthor = comment.UserId == userIdResult.Value;
        if (!isAuthor) {
            Recipe? recipe = await recipeAccessService.GetAccessibleByIdAsync(
                recipeId, userIdResult.Value, includePublic: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (recipe is null) {
                return Result.Failure(Errors.RecipeComment.NotAuthor);
            }
        }

        await commentRepository.DeleteAsync(comment, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
