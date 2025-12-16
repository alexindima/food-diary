using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IImageAssetCleanupService
{
    Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
    Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default);
}

public sealed record DeleteImageAssetResult(bool Deleted, string? ErrorCode = null);

