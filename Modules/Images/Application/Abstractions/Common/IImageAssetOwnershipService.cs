using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetOwnershipService {
    Task ReassignAsync(IReadOnlyCollection<ImageAssetId> assetIds, UserId targetUserId, CancellationToken cancellationToken);
}
