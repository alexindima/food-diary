using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Infrastructure;

internal sealed class UserProfileImageService(
    IImageAssetAccessService accessService,
    IImageAssetCleanupService cleanupService) : IUserProfileImageService {
    public async Task<Result<string?>> ResolveOptionalUrlAsync(ImageAssetId? assetId, UserId userId, CancellationToken cancellationToken = default) {
        Result<ImageAsset?> result = await accessService.ResolveOptionalAsync(assetId, userId, cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<string?>(result.Error) : Result.Success(result.Value?.Url);
    }

    public Task DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
        cleanupService.DeleteIfUnusedAsync(assetId, cancellationToken);
}
