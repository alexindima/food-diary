using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageAssetCleanupBatch {
    Task<bool> DeleteUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
}
