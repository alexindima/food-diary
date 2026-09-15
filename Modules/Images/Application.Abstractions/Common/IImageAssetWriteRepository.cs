using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageAssetWriteRepository {
    Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default);

    Task<ImageAsset?> GetOwnedByIdAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default);

    Task<ImageAsset?> GetOwnedForUpdateAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default);

    Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);

    Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default);

    Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default);
}
