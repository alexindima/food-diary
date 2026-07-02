using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;

public class ToggleRecipeLikeCommandHandler(
    IRecipeLikeRepository likeRepository,
    IRecipeAccessService recipeAccessService)
    : ICommandHandler<ToggleRecipeLikeCommand, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        ToggleRecipeLikeCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeLikeStatusModel>(userIdResult.Error);
        }

        var recipeId = (RecipeId)command.RecipeId;
        Recipe? recipe = await recipeAccessService.GetAccessibleByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure<RecipeLikeStatusModel>(Errors.Recipe.NotFound(command.RecipeId));
        }

        RecipeLike? existingLike = await likeRepository.GetByUserAndRecipeAsync(
            userIdResult.Value, recipeId, cancellationToken).ConfigureAwait(false);
        int currentTotalLikes = await likeRepository.CountByRecipeAsync(recipeId, cancellationToken).ConfigureAwait(false);

        bool isLiked;
        int totalLikes;
        if (existingLike is not null) {
            await likeRepository.DeleteAsync(existingLike, cancellationToken).ConfigureAwait(false);
            isLiked = false;
            totalLikes = Math.Max(0, currentTotalLikes - 1);
        } else {
            var like = RecipeLike.Create(userIdResult.Value, recipeId);
            await likeRepository.AddAsync(like, cancellationToken).ConfigureAwait(false);
            isLiked = true;
            totalLikes = currentTotalLikes + 1;
        }

        return Result.Success(new RecipeLikeStatusModel(isLiked, totalLikes));
    }
}
