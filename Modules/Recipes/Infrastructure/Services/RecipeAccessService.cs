using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services;

public sealed class RecipeAccessService(IRecipeOverviewReadService recipeReadService) : IRecipeAccessService {
    public async Task<RecipeOverviewReadItem?> GetAccessibleByIdAsync(
        RecipeId recipeId,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> recipes = await recipeReadService
            .GetByIdsWithUsageAsync([recipeId], userId, includePublic, cancellationToken)
            .ConfigureAwait(false);
        return recipes.GetValueOrDefault(recipeId);
    }
}
