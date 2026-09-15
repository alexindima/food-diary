using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentRecipes;

namespace FoodDiary.Modules.Recipes.Application.Services;

public sealed class RecentRecipeLoader(ISender sender, IRecipeOverviewReadService recipeOverviewReadService) {
    public async Task<IReadOnlyList<RecipeOverviewReadItem>> LoadAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken) {
        IReadOnlyList<RecentRecipeUsage> recents = await sender.Send(new ReadRecentRecipesQuery(userId, limit), cancellationToken)
            .ConfigureAwait(false);
        if (recents.Count == 0) {
            return [];
        }

        RecipeId[] idsInOrder = [.. recents.Select(recent => recent.RecipeId)];
        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> itemsById = await recipeOverviewReadService
            .GetByIdsWithUsageAsync(idsInOrder, userId, includePublic, cancellationToken)
            .ConfigureAwait(false);

        return [.. idsInOrder.Where(itemsById.ContainsKey).Select(id => itemsById[id])];
    }
}
