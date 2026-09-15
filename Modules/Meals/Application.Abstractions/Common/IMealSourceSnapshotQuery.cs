using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Application.Abstractions.Models;

namespace FoodDiary.Modules.Meals.Application.Abstractions.Common;

public interface IMealSourceSnapshotQuery {
    Task<IReadOnlyDictionary<ImageAssetId, string>> GetImageUrlsAsync(
        IReadOnlyCollection<ImageAssetId> imageAssetIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, MealRecipeSourceReadModel>> GetLegacyRecipesAsync(
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);
}
