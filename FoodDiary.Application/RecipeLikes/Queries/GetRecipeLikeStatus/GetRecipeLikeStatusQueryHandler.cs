using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public sealed class GetRecipeLikeStatusQueryHandler(IRecipeLikeReadService likeReadService)
    : IQueryHandler<GetRecipeLikeStatusQuery, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        GetRecipeLikeStatusQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<RecipeLikeStatusModel>(userIdResult);
        }

        var recipeId = (RecipeId)query.RecipeId;
        RecipeLikeStatusModel status = await likeReadService
            .GetStatusAsync(userIdResult.Value, recipeId, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(status);
    }
}
