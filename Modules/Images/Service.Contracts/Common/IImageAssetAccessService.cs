using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public interface IImageAssetAccessService {
    Task<Result<ImageAssetReadModel?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
