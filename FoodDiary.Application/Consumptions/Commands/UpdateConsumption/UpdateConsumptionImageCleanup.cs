using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

internal static class UpdateConsumptionImageCleanup {
    public static async Task DeleteOldImageAssetAsync(
        UpdateConsumptionCommand command,
        ImageAssetId? oldAssetId,
        IImageAssetCleanupService imageAssetCleanupService,
        CancellationToken cancellationToken) {
        bool imageAssetChanged = command.ImageAssetId.HasValue &&
                                (!oldAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value);

        if (oldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
