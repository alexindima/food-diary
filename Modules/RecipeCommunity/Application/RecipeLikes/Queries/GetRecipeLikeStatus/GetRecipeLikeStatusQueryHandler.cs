using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.RecipeCommunity.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public sealed class GetRecipeLikeStatusQueryHandler(
    IRecipeLikeReadRepository likeRepository,
    IRecipeAccessService recipeAccessService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetRecipeLikeStatusQuery, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        GetRecipeLikeStatusQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<RecipeLikeStatusModel>(userIdResult);
        }

        var recipeId = (RecipeId)query.RecipeId;
        if (await recipeAccessService.GetAccessibleByIdAsync(
            recipeId, userIdResult.Value, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false) is null) {
            return Result.Failure<RecipeLikeStatusModel>(RecipeErrors.NotFound(query.RecipeId));
        }

        RecipeLikeStatusModel status = await GetStatusAsync(userIdResult.Value, recipeId, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(status);
    }
    private async Task<RecipeLikeStatusModel> GetStatusAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken) {
        bool isLiked = await likeRepository
            .ExistsByUserAndRecipeAsync(userId, recipeId, cancellationToken)
            .ConfigureAwait(false);
        int totalLikes = await likeRepository.CountByRecipeAsync(recipeId, cancellationToken).ConfigureAwait(false);

        return new RecipeLikeStatusModel(isLiked, totalLikes);
    }
}
