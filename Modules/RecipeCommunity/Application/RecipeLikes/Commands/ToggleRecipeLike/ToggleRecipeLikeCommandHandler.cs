using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Models;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Commands.ToggleRecipeLike;

public sealed class ToggleRecipeLikeCommandHandler(
    IRecipeLikeWriteRepository likeRepository,
    IRecipeAccessService recipeAccessService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<ToggleRecipeLikeCommand, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        ToggleRecipeLikeCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeLikeStatusModel>(userIdResult);
        }

        var recipeId = (RecipeId)command.RecipeId;
        RecipeOverviewReadItem? recipe = await recipeAccessService.GetAccessibleByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure<RecipeLikeStatusModel>(RecipeErrors.NotFound(command.RecipeId));
        }

        RecipeLike? existingLike = await likeRepository.GetByUserAndRecipeAsync(
            userIdResult.Value, recipeId, cancellationToken).ConfigureAwait(false);
        int currentTotalLikes = await likeRepository.CountByRecipeAsync(recipeId, cancellationToken).ConfigureAwait(false);

        if (existingLike is not null && !command.IsLiked) {
            await likeRepository.DeleteAsync(existingLike, cancellationToken).ConfigureAwait(false);
            currentTotalLikes = Math.Max(0, currentTotalLikes - 1);
        } else if (existingLike is null && command.IsLiked) {
            var like = RecipeLike.Create(userIdResult.Value, recipeId);
            await likeRepository.AddAsync(like, cancellationToken).ConfigureAwait(false);
            currentTotalLikes++;
        }

        return Result.Success(new RecipeLikeStatusModel(command.IsLiked, currentTotalLikes));
    }
}
