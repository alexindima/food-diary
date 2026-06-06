using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;

public class CreateRecipeCommentCommandHandler(
    IRecipeCommentRepository commentRepository,
    IRecipeRepository recipeRepository,
    INotificationRepository notificationRepository)
    : ICommandHandler<CreateRecipeCommentCommand, Result<RecipeCommentModel>> {
    public async Task<Result<RecipeCommentModel>> Handle(
        CreateRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeCommentModel>(userIdResult.Error);
        }

        var recipeId = (RecipeId)command.RecipeId;
        Recipe? recipe = await recipeRepository.GetByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure<RecipeCommentModel>(Errors.Recipe.NotFound(command.RecipeId));
        }

        var comment = RecipeComment.Create(userIdResult.Value, recipeId, command.Text);
        await commentRepository.AddAsync(comment, cancellationToken).ConfigureAwait(false);

        // Notify recipe owner (unless commenting on own recipe)
        if (recipe.UserId != userIdResult.Value) {
            Notification notification = NotificationFactory.CreateNewComment(
                recipe.UserId,
                recipe.Id.Value.ToString());
            await notificationRepository.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        }

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
