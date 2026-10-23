using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public interface IImageAssetOwnershipService {
    /// <summary>Transfers only requested assets owned by the source user within the caller's transaction.</summary>
    Task ReassignAsync(IReadOnlyCollection<ImageAssetId> assetIds, UserId sourceUserId, UserId targetUserId, CancellationToken cancellationToken);
}
