using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public interface IImageAssetOwnershipService {
    Task ReassignAsync(IReadOnlyCollection<ImageAssetId> assetIds, UserId targetUserId, CancellationToken cancellationToken);
}
