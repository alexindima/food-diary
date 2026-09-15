using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Contracts.Common;

public interface IRecipeAccessService {
    Task<RecipeOverviewReadItem?> GetAccessibleByIdAsync(
        RecipeId recipeId,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);
}
