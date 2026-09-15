using FoodDiary.Modules.Images.Application.Abstractions.Models;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageAssetUsageQuery {
    Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImageCleanupCandidate>> GetUnusedCandidatesOlderThanAsync(
        DateTime olderThanUtc, int batchSize, ImageCleanupCandidate? after = null, CancellationToken cancellationToken = default);
}
