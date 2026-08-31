using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Common;
using FoodDiary.Application.RecipeCommunity.RecipeLikes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecipeCommunity.RecipeLikes.Services;

public sealed class RecipeLikeReadService(IRecipeLikeReadRepository likeRepository)
    : IRecipeLikeReadService {
    public async Task<RecipeLikeStatusModel> GetStatusAsync(
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
