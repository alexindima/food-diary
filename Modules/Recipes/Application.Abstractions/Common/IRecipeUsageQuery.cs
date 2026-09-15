using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Abstractions.Common;

public interface IRecipeUsageQuery {
    Task<int> GetUsageCountAsync(RecipeId id, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default);
}
