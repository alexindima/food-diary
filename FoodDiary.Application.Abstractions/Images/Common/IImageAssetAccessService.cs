using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetAccessService {
    Task<Result<ImageAsset?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
