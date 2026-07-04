using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services;

public sealed class RecipeAccessService(IRecipeReadRepository recipeRepository) : IRecipeAccessService {
    public Task<Recipe?> GetAccessibleByIdAsync(
        RecipeId recipeId,
        UserId userId,
        bool includePublic = true,
        bool includeSteps = false,
        CancellationToken cancellationToken = default) =>
        recipeRepository.GetByIdAsync(recipeId, userId, includePublic, includeSteps, cancellationToken: cancellationToken);
}
