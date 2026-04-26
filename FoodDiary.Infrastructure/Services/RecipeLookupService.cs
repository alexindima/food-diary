using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services;

public class RecipeLookupService(IRecipeRepository recipeRepository) : IRecipeLookupService {
    public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
        IEnumerable<RecipeId> ids,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        recipeRepository.GetByIdsAsync(ids, userId, includePublic: true, cancellationToken);
}
