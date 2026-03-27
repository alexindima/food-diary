using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Common;

public interface IImageAssetCleanupService {
    Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
    Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);
}

public sealed record DeleteImageAssetResult(bool Deleted, string? ErrorCode = null);
