using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Social;

namespace FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public class GetRecipeLikeStatusQueryHandler(IRecipeLikeRepository likeRepository)
    : IQueryHandler<GetRecipeLikeStatusQuery, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        GetRecipeLikeStatusQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeLikeStatusModel>(userIdResult.Error);
        }

        var recipeId = (RecipeId)query.RecipeId;
        RecipeLike? existingLike = await likeRepository.GetByUserAndRecipeAsync(
            userIdResult.Value, recipeId, cancellationToken).ConfigureAwait(false);

        int totalLikes = await likeRepository.CountByRecipeAsync(recipeId, cancellationToken).ConfigureAwait(false);
        return Result.Success(new RecipeLikeStatusModel(existingLike is not null, totalLikes));
    }
}
