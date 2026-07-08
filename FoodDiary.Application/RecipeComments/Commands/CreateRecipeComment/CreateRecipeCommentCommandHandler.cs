using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;

public sealed class CreateRecipeCommentCommandHandler(
    IRecipeCommentWriteRepository commentRepository,
    IRecipeAccessService recipeAccessService,
    INotificationWriter notificationWriter)
    : ICommandHandler<CreateRecipeCommentCommand, Result<RecipeCommentModel>> {
    public async Task<Result<RecipeCommentModel>> Handle(
        CreateRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<RecipeCommentModel>(userIdResult);
        }

        var recipeId = (RecipeId)command.RecipeId;
        Recipe? recipe = await recipeAccessService.GetAccessibleByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure<RecipeCommentModel>(Errors.Recipe.NotFound(command.RecipeId));
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

        Notification notification = NotificationFactory.CreateNewComment(
            recipe.UserId,
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
