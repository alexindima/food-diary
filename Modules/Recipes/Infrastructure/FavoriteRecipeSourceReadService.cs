using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure;

internal sealed class FavoriteRecipeSourceReadService(IRecipeAccessService source) : IFavoriteRecipeSourceReadService {
    public async Task<Result<FavoriteRecipeSourceModel>> GetAccessibleAsync(RecipeId id, UserId userId, CancellationToken cancellationToken = default) {
        RecipeOverviewReadItem? item = await source.GetAccessibleByIdAsync(id, userId, includePublic: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return item is null
            ? Result.Failure<FavoriteRecipeSourceModel>(RecipeErrors.NotFound(id.Value))
            : Result.Success(new FavoriteRecipeSourceModel(
            item.Name,
            item.ImageUrl,
            item.TotalCalories,
            item.ManualCalories,
            item.Servings,
            item.PrepTime,
            item.CookTime,
            item.Steps.Sum(step => step.Ingredients.Count)));
    }
}
