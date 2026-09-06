using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserProfileImageService {
    Task<Result<string?>> ResolveOptionalUrlAsync(ImageAssetId? assetId, UserId userId, CancellationToken cancellationToken = default);
    Task DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default);
}
