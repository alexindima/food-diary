using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.UpdateRecipeComment;

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

        var recipeId = (RecipeId)command.RecipeId;
        var commentId = (RecipeCommentId)command.CommentId;
        RecipeComment? comment = await commentRepository.GetByIdAsync(commentId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (comment is null || comment.RecipeId != recipeId) {
            return Result.Failure<RecipeCommentModel>(RecipeCommentErrors.NotFound(command.CommentId));
        }

        if (comment.UserId != userIdResult.Value) {
            return Result.Failure<RecipeCommentModel>(RecipeCommentErrors.NotAuthor);
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
