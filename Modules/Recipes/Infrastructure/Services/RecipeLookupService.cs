using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services;

public sealed class RecipeLookupService(IRecipeOverviewReadService recipeReadService) : IRecipeLookupService {
    public Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetAccessibleByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        recipeReadService.GetByIdsWithUsageAsync(ids, userId, includePublic: true, cancellationToken);
}
