using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Infrastructure.Services;

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
