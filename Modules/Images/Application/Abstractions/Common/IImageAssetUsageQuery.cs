using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetUsageQuery {
    Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImageAssetId>> GetUnusedIdsOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);
}
