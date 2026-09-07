using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class UserProfileImageServiceTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveOptionalUrlAsync_MapsOptionalAssetAndForwardsScope(bool hasAsset) {
        var id = ImageAssetId.New();
        var user = UserId.New();
        using var cancellation = new CancellationTokenSource();
        var access = new AccessService(Result.Success<ImageAssetReadModel?>(hasAsset ? new(id, "https://image.test/a") : null));
        var service = new UserProfileImageService(access, new CleanupService());

        Result<string?> result = await service.ResolveOptionalUrlAsync(hasAsset ? id : null, user, cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Multiple(
            () => Assert.Equal(hasAsset ? "https://image.test/a" : null, result.Value),
            () => Assert.Equal((hasAsset ? (ImageAssetId?)id : null, user, cancellation.Token), access.Call));
    }

    [Fact]
    public async Task ResolveOptionalUrlAsync_PreservesAccessFailure() {
        var error = new Error("image.denied", "Denied", ErrorKind.Forbidden);
        var service = new UserProfileImageService(new AccessService(Result.Failure<ImageAssetReadModel?>(error)), new CleanupService());
        Result<string?> result = await service.ResolveOptionalUrlAsync(ImageAssetId.New(), UserId.New());
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task DeleteIfUnusedAsync_DelegatesCleanup() {
        var cleanup = new CleanupService();
        var service = new UserProfileImageService(new AccessService(Result.Success<ImageAssetReadModel?>(value: null)), cleanup);
        var id = ImageAssetId.New();
        using var cancellation = new CancellationTokenSource();
        await service.DeleteIfUnusedAsync(id, cancellation.Token);
        Assert.Equal((id, cancellation.Token), cleanup.Call);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AccessService(Result<ImageAssetReadModel?> result) : IImageAssetAccessService {
        public (ImageAssetId?, UserId, CancellationToken) Call { get; private set; }
        public Task<Result<ImageAssetReadModel?>> ResolveOptionalAsync(ImageAssetId? assetId, UserId userId, CancellationToken cancellationToken = default) {
            Call = (assetId, userId, cancellationToken);
            return Task.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class CleanupService : IImageAssetCleanupService {
        public (ImageAssetId, CancellationToken) Call { get; private set; }
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            Call = (assetId, cancellationToken);
            return Task.FromResult(new DeleteImageAssetResult(Deleted: true));
        }
        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
