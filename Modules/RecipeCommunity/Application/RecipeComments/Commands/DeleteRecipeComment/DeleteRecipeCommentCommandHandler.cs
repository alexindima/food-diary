using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.DeleteRecipeComment;

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
            return Result.Failure(RecipeCommentErrors.NotFound(command.CommentId));
        }

        // Author or recipe owner can delete
        bool isAuthor = comment.UserId == userIdResult.Value;
        if (!isAuthor) {
            RecipeOverviewReadItem? recipe = await recipeAccessService.GetAccessibleByIdAsync(
                recipeId, userIdResult.Value, includePublic: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (recipe is null) {
                return Result.Failure(RecipeCommentErrors.NotAuthor);
            }
        }

        await commentRepository.DeleteAsync(comment, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
