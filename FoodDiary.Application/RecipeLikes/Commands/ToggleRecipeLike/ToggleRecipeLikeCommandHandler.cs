using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;

public class ToggleRecipeLikeCommandHandler(
    IRecipeLikeRepository likeRepository,
    IRecipeRepository recipeRepository)
    : ICommandHandler<ToggleRecipeLikeCommand, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        ToggleRecipeLikeCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeLikeStatusModel>(userIdResult.Error);
        }

        var recipeId = (RecipeId)command.RecipeId;
        var recipe = await recipeRepository.GetByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken);

        if (recipe is null) {
            return Result.Failure<RecipeLikeStatusModel>(Errors.Recipe.NotFound(command.RecipeId));
        }

        var existingLike = await likeRepository.GetByUserAndRecipeAsync(
            userIdResult.Value, recipeId, cancellationToken);

        bool isLiked;
        if (existingLike is not null) {
            await likeRepository.DeleteAsync(existingLike, cancellationToken);
            isLiked = false;
        } else {
            var like = RecipeLike.Create(userIdResult.Value, recipeId);
            await likeRepository.AddAsync(like, cancellationToken);
            isLiked = true;
        }

        var totalLikes = await likeRepository.CountByRecipeAsync(recipeId, cancellationToken);
        return Result.Success(new RecipeLikeStatusModel(isLiked, totalLikes));
    }
}
