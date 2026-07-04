using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetReadRepository {
    Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default);

    Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
