using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Abstractions.Common;

public interface IImageAssetReadRepository {
    Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default);

    Task<ImageAsset?> GetOwnedByIdAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default);

    Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);

}
