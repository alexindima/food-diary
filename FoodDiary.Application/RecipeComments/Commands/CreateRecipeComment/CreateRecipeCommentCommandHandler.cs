using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.RecipeComments.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeComments.Commands.CreateRecipeComment;

public class CreateRecipeCommentCommandHandler(
    IRecipeCommentRepository commentRepository,
    IRecipeRepository recipeRepository,
    INotificationRepository notificationRepository)
    : ICommandHandler<CreateRecipeCommentCommand, Result<RecipeCommentModel>> {
    public async Task<Result<RecipeCommentModel>> Handle(
        CreateRecipeCommentCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeCommentModel>(userIdResult.Error);
        }

        var recipeId = (RecipeId)command.RecipeId;
        var recipe = await recipeRepository.GetByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken);

        if (recipe is null) {
            return Result.Failure<RecipeCommentModel>(Errors.Recipe.NotFound(command.RecipeId));
        }

        var comment = RecipeComment.Create(userIdResult.Value, recipeId, command.Text);
        await commentRepository.AddAsync(comment, cancellationToken);

        // Notify recipe owner (unless commenting on own recipe)
        if (recipe.UserId != userIdResult.Value) {
            var notification = NotificationFactory.CreateNewComment(
                recipe.UserId,
                recipe.Id.Value.ToString());
            await notificationRepository.AddAsync(notification, cancellationToken);
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
