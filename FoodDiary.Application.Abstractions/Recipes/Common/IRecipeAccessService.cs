using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Recipes.Common;

public interface IRecipeAccessService {
    Task<Recipe?> GetAccessibleByIdAsync(
        RecipeId recipeId,
        UserId userId,
        bool includePublic = true,
        bool includeSteps = false,
        CancellationToken cancellationToken = default);
}
