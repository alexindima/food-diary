using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Models;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeComments.Commands.CreateRecipeComment;

public sealed class CreateRecipeCommentCommandHandler(
    IRecipeCommentWriteRepository commentRepository,
    IRecipeAccessService recipeAccessService,
    INotificationWriter notificationWriter,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateRecipeCommentCommand, Result<RecipeCommentModel>> {
    public async Task<Result<RecipeCommentModel>> Handle(
        CreateRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeCommentModel>(userIdResult);
        }

        var recipeId = (RecipeId)command.RecipeId;
        RecipeOverviewReadItem? recipe = await recipeAccessService.GetAccessibleByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure<RecipeCommentModel>(RecipeErrors.NotFound(command.RecipeId));
        }

        var comment = RecipeComment.Create(userIdResult.Value, recipeId, command.Text);
        await commentRepository.AddAsync(comment, cancellationToken).ConfigureAwait(false);

        // Notify recipe owner (unless commenting on own recipe)
        if (recipe.UserId == userIdResult.Value) {
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

        var notification = new NotificationRequest(
            recipe.UserId,
            NotificationTypes.NewComment,
            NotificationPayloads.Empty(),
            recipe.Id.Value.ToString());
        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);

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
