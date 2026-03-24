using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Images.Commands.DeleteImageAsset;
using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Application.Images.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Images;

public class ImagesFeatureTests {
    [Fact]
    public async Task GetImageUploadUrlCommandHandler_WithEmptyUserId_ReturnsFailure() {
        var handler = new GetImageUploadUrlCommandHandler(
            new FakeImageStorageService(),
            new FakeImageAssetRepository());

        var command = new GetImageUploadUrlCommand(UserId.Empty, "file.jpg", "image/jpeg", 100);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidData", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithOtherOwner_ReturnsForbidden() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var anotherUser = UserId.New();
        var asset = ImageAsset.Create(owner, "images/a.jpg", "https://cdn/a.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var handler = new DeleteImageAssetCommandHandler(repo, new FakeCleanupService());
        var result = await handler.Handle(new DeleteImageAssetCommand(anotherUser, asset.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ImageAssetCleanupService_CleanupOrphans_WithNonPositiveBatch_ReturnsZero() {
        var service = new ImageAssetCleanupService(
            new FakeImageAssetRepository(),
            new FakeImageStorageService(),
            NullLogger<ImageAssetCleanupService>.Instance);

        var removed = await service.CleanupOrphansAsync(DateTime.UtcNow, 0, CancellationToken.None);

        Assert.Equal(0, removed);
    }

    [Fact]
    public async Task ImageAssetCleanupService_DeleteIfUnused_WhenInUse_ReturnsInUse() {
        var repo = new FakeImageAssetRepository();
        var asset = ImageAsset.Create(UserId.New(), "images/in-use.jpg", "https://cdn/in-use.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        repo.InUseIds.Add(asset.Id);

        var service = new ImageAssetCleanupService(
            repo,
            new FakeImageStorageService(),
            NullLogger<ImageAssetCleanupService>.Instance);

        var result = await service.DeleteIfUnusedAsync(asset.Id, CancellationToken.None);

        Assert.False(result.Deleted);
        Assert.Equal("in_use", result.ErrorCode);
    }

    private sealed class FakeCleanupService : IImageAssetCleanupService {
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(true));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FakeImageStorageService : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(
            UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) {
            return Task.FromResult(new PresignedUpload(
                "https://upload.example",
                "https://cdn.example/file.jpg",
                "images/file.jpg",
                DateTime.UtcNow.AddMinutes(10)));
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeImageAssetRepository : IImageAssetRepository {
        private readonly Dictionary<ImageAssetId, ImageAsset> _assets = [];
        public HashSet<ImageAssetId> InUseIds { get; } = [];

        public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
            _assets[asset.Id] = asset;
            return Task.FromResult(asset);
        }

        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) {
            _assets.TryGetValue(id, out var asset);
            return Task.FromResult(asset);
        }

        public Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
            _assets.Remove(asset.Id);
            return Task.CompletedTask;
        }

        public Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(InUseIds.Contains(assetId));

        public Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            var result = _assets.Values
                .Where(a => a.CreatedOnUtc <= olderThanUtc && !InUseIds.Contains(a.Id))
                .Take(batchSize)
                .ToList();
            return Task.FromResult<IReadOnlyList<ImageAsset>>(result);
        }
    }
}
