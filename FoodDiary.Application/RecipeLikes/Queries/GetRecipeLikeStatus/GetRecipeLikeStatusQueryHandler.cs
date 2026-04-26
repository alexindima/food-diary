using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;

public class GetRecipeLikeStatusQueryHandler(IRecipeLikeRepository likeRepository)
    : IQueryHandler<GetRecipeLikeStatusQuery, Result<RecipeLikeStatusModel>> {
    public async Task<Result<RecipeLikeStatusModel>> Handle(
        GetRecipeLikeStatusQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<RecipeLikeStatusModel>(userIdResult.Error);
        }

        var recipeId = (RecipeId)query.RecipeId;
        var existingLike = await likeRepository.GetByUserAndRecipeAsync(
            userIdResult.Value, recipeId, cancellationToken);

        var totalLikes = await likeRepository.CountByRecipeAsync(recipeId, cancellationToken);
        return Result.Success(new RecipeLikeStatusModel(existingLike is not null, totalLikes));
    }
}
