using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeLookupService {
    Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        CancellationToken cancellationToken = default);
}
