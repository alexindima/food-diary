using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeUsageQuery {
    Task<int> GetUsageCountAsync(RecipeId id, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default);
}
