using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Modules.Recipes.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Recipes.Infrastructure;

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
