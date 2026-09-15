using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Infrastructure.Services;

public sealed class RecipeLookupService(IRecipeOverviewReadService recipeReadService) : IRecipeLookupService {
    public Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetAccessibleByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        recipeReadService.GetByIdsWithUsageAsync(ids, userId, includePublic: true, cancellationToken);
}
