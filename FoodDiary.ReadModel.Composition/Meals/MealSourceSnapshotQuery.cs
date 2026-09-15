using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Application.Abstractions.Models;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Meals;

public sealed class MealSourceSnapshotQuery(ICompositionReadContext context) : IMealSourceSnapshotQuery {
    public async Task<IReadOnlyDictionary<ImageAssetId, string>> GetImageUrlsAsync(
        IReadOnlyCollection<ImageAssetId> imageAssetIds,
        CancellationToken cancellationToken = default) {
        if (imageAssetIds.Count == 0) {
            return new Dictionary<ImageAssetId, string>();
        }
        ImageAssetId[] ids = [.. imageAssetIds];
        return await context.ImageAssets
            .AsNoTracking()
            .Where(asset => ((IEnumerable<ImageAssetId>)ids).Contains(asset.Id))
            .Select(asset => new { asset.Id, asset.Url })
            .ToDictionaryAsync(asset => asset.Id, asset => asset.Url, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<RecipeId, MealRecipeSourceReadModel>> GetLegacyRecipesAsync(
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default) {
        if (recipeIds.Count == 0) {
            return new Dictionary<RecipeId, MealRecipeSourceReadModel>();
        }
        RecipeId[] ids = [.. recipeIds];
        return await context.Recipes
            .AsNoTracking()
            .Where(recipe => ((IEnumerable<RecipeId>)ids).Contains(recipe.Id))
            .Select(recipe => new {
                recipe.Id, recipe.Name, recipe.ImageUrl, recipe.Servings,
                recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats,
                recipe.TotalCarbs, recipe.TotalFiber, recipe.TotalAlcohol,
            })
            .ToDictionaryAsync(recipe => recipe.Id, recipe => new MealRecipeSourceReadModel(
                recipe.Name, recipe.ImageUrl, recipe.Servings, recipe.TotalCalories,
                recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs,
                recipe.TotalFiber, recipe.TotalAlcohol), cancellationToken)
            .ConfigureAwait(false);
    }
}
