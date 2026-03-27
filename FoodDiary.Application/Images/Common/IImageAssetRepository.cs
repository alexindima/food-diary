using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Common;

public interface IImageAssetRepository {
    Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default);

    Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default);

    Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default);

    Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
