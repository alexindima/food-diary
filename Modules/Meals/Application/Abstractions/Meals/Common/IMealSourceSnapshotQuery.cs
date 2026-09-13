using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Meals.Common;

public interface IMealSourceSnapshotQuery {
    Task<IReadOnlyDictionary<ImageAssetId, string>> GetImageUrlsAsync(
        IReadOnlyCollection<ImageAssetId> imageAssetIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<RecipeId, MealRecipeSourceReadModel>> GetLegacyRecipesAsync(
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);
}
