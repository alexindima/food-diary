using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Contracts.Common;

public interface IRecipeLookupService {
    Task<IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem>> GetAccessibleByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        CancellationToken cancellationToken = default);
}
