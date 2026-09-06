using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetAccessService {
    Task<Result<ImageAssetReadModel?>> ResolveOptionalAsync(
        ImageAssetId? assetId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
