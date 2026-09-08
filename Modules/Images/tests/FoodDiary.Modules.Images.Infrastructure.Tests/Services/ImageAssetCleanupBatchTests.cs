using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class ImageAssetCleanupBatchTests {
    [Fact]
    public async Task ReassignAsync_EmptyAssetsDoesNotAccessPersistence() {
        var service = new ImageAssetOwnershipService(null!);
        await service.ReassignAsync([], UserId.New(), CancellationToken.None);
    }
    [Fact]
    public async Task DeleteUnusedAsync_WhenAssetIsRetained_DisposesScopeWithoutSaving() {
        var cleanup = new RetainingCleanupService();
        var services = new ServiceCollection();
        services.AddScoped<IImageAssetCleanupService>(_ => cleanup);
        await using ServiceProvider provider = services.BuildServiceProvider();
        var batch = new ImageAssetCleanupBatch(provider.GetRequiredService<IServiceScopeFactory>());
        var id = ImageAssetId.New();
        using var cancellation = new CancellationTokenSource();

        bool deleted = await batch.DeleteUnusedAsync(id, cancellation.Token);

        Assert.False(deleted);
        Assert.Equal((id, cancellation.Token), cleanup.Call);
        Assert.True(cleanup.Disposed);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RetainingCleanupService : IImageAssetCleanupService, IAsyncDisposable {
        public (ImageAssetId, CancellationToken)? Call { get; private set; }
        public bool Disposed { get; private set; }

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            Call = (assetId, cancellationToken);
            return Task.FromResult(new DeleteImageAssetResult(Deleted: false));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask DisposeAsync() {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
