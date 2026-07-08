using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public sealed class GetRecipeLikeStatusQueryHandler(
    IRecipeLikeReadService likeReadService,
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
        RecipeLikeStatusModel status = await likeReadService
            .GetStatusAsync(userIdResult.Value, recipeId, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(status);
    }
}
