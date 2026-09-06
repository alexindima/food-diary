using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetCleanupBatch {
    Task<bool> DeleteUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
}
