using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

internal static class RecipeUpdateAssetCleanup {
    public static async Task DeleteUnusedAsync(
        UpdateRecipeCommand command,
        UpdateRecipeValues values,
        IImageAssetCleanupService imageAssetCleanupService,
        CancellationToken cancellationToken) {
        bool imageAssetChanged = command.ClearImageAssetId ||
                                (command.ImageAssetId.HasValue &&
                                 (!values.OldAssetId.HasValue || values.OldAssetId.Value.Value != command.ImageAssetId.Value));

        if (values.OldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(values.OldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }

        if (values.OldStepAssetIds.Count == 0) {
            return;
        }

        var newStepAssetIds = values.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => new ImageAssetId(id!.Value))
            .ToHashSet();

        foreach (ImageAssetId assetId in values.OldStepAssetIds) {
            if (!newStepAssetIds.Contains(assetId)) {
                await imageAssetCleanupService.DeleteIfUnusedAsync(assetId, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
