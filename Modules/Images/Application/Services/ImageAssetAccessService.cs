using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Application.Services;

public sealed class ImageAssetAccessService(
    IImageAssetReadRepository imageAssetRepository) : IImageAssetAccessService {
    public async Task<Result<ImageAssetReadModel?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default) {
        if (!assetId.HasValue) {
            return Result.Success<ImageAssetReadModel?>(value: null);
        }

        ImageAsset? asset = await imageAssetRepository.GetOwnedByIdAsync(assetId.Value, userId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<ImageAssetReadModel?>(ImageErrors.NotFound(assetId.Value.Value));
        }

        if (!asset.IsConfirmed) {
            return Result.Failure<ImageAssetReadModel?>(ImageErrors.InvalidData(
                "Image upload has not been confirmed."));
        }

        return Result.Success<ImageAssetReadModel?>(new ImageAssetReadModel(asset.Id, asset.Url));
    }
}
