using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Infrastructure;

internal sealed class UserProfileImageService(
    IImageAssetAccessService accessService,
    IImageAssetCleanupService cleanupService) : IUserProfileImageService {
    public async Task<Result<string?>> ResolveOptionalUrlAsync(ImageAssetId? assetId, UserId userId, CancellationToken cancellationToken = default) {
        Result<ImageAssetReadModel?> result = await accessService.ResolveOptionalAsync(assetId, userId, cancellationToken).ConfigureAwait(false);
        return result.IsFailure ? Result.Failure<string?>(result.Error) : Result.Success(result.Value?.Url);
    }

    public Task DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
        cleanupService.DeleteIfUnusedAsync(assetId, cancellationToken);
}
