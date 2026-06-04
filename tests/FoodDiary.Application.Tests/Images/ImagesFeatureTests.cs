using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Images.Commands.DeleteImageAsset;
using FoodDiary.Application.Images.Commands.GetUploadUrl;
using FoodDiary.Application.Images.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Images;

[ExcludeFromCodeCoverage]
public class ImagesFeatureTests {
    [Fact]
    public async Task GetImageUploadUrlCommandHandler_WithEmptyUserId_ReturnsFailure() {
        var handler = new GetImageUploadUrlCommandHandler(
            new FakeImageStorageService(),
            new FakeImageAssetRepository());

        var command = new GetImageUploadUrlCommand(Guid.Empty, "file.jpg", "image/jpeg", 100);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidData", result.Error.Code);
    }

    [Fact]
    public async Task GetImageUploadUrlCommandHandler_WithValidRequest_PersistsAssetAndReturnsPresignedUrl() {
        var repository = new FakeImageAssetRepository();
        var handler = new GetImageUploadUrlCommandHandler(
            new FakeImageStorageService(),
            repository);

        var result = await handler.Handle(
            new GetImageUploadUrlCommand(Guid.NewGuid(), "photo.jpg", "image/jpeg", 1024),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://upload.example", result.Value.UploadUrl);
        Assert.Equal("https://cdn.example/file.jpg", result.Value.FileUrl);
        Assert.NotEqual(Guid.Empty, result.Value.AssetId);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WhenAssetMissing_ReturnsNotFound() {
        var handler = new DeleteImageAssetCommandHandler(
            new FakeImageAssetRepository(),
            new FakeCleanupService());

        var assetId = Guid.NewGuid();
        var result = await handler.Handle(new DeleteImageAssetCommand(Guid.NewGuid(), assetId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithOtherOwner_ReturnsForbidden() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var anotherUser = UserId.New();
        var asset = ImageAsset.Create(owner, "images/a.jpg", "https://cdn/a.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var handler = new DeleteImageAssetCommandHandler(repo, new FakeCleanupService());
        var result = await handler.Handle(new DeleteImageAssetCommand(anotherUser.Value, asset.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WhenCleanupReturnsStorageError_ReturnsStorageError() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/a.jpg", "https://cdn/a.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var handler = new DeleteImageAssetCommandHandler(repo, new FakeCleanupService("storage_error"));
        var result = await handler.Handle(new DeleteImageAssetCommand(owner.Value, asset.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.StorageError", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithEmptyAssetId_ReturnsInvalidDataFailure() {
        var handler = new DeleteImageAssetCommandHandler(
            new FakeImageAssetRepository(),
            new FakeCleanupService());

        var result = await handler.Handle(new DeleteImageAssetCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("AssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithEmptyUserId_ReturnsInvalidDataFailure() {
        var handler = new DeleteImageAssetCommandHandler(
            new FakeImageAssetRepository(),
            new FakeCleanupService());

        var result = await handler.Handle(new DeleteImageAssetCommand(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task ImageAssetCleanupService_CleanupOrphans_DeletesEligibleAssetsAndKeepsInUseAssets() {
        var repository = new FakeImageAssetRepository();
        var removable = ImageAsset.Create(UserId.New(), "images/removable.jpg", "https://cdn/removable.jpg");
        var inUse = ImageAsset.Create(UserId.New(), "images/in-use.jpg", "https://cdn/in-use.jpg");
        await repository.AddAsync(removable, CancellationToken.None);
        await repository.AddAsync(inUse, CancellationToken.None);
        repository.InUseIds.Add(inUse.Id);
        var service = new ImageAssetCleanupService(
            repository,
            new FakeImageStorageService(),
            NullLogger<ImageAssetCleanupService>.Instance);

        var removed = await service.CleanupOrphansAsync(
            DateTime.UtcNow.AddYears(1),
            10,
            CancellationToken.None);

        Assert.Equal(1, removed);
        Assert.Null(await repository.GetByIdAsync(removable.Id, CancellationToken.None));
        Assert.NotNull(await repository.GetByIdAsync(inUse.Id, CancellationToken.None));
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

    [Fact]
    public async Task ImageAssetCleanupService_WhenStorageDeleteFails_ReturnsStorageErrorWithoutDeletingRepositoryAsset() {
        var repo = new FakeImageAssetRepository();
        var asset = ImageAsset.Create(UserId.New(), "images/fail.jpg", "https://cdn/fail.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var service = new ImageAssetCleanupService(
            repo,
            new ThrowingImageStorageService(),
            NullLogger<ImageAssetCleanupService>.Instance);

        var result = await service.DeleteIfUnusedAsync(asset.Id, CancellationToken.None);
        var storedAsset = await repo.GetByIdAsync(asset.Id, CancellationToken.None);

        Assert.False(result.Deleted);
        Assert.Equal("storage_error", result.ErrorCode);
        Assert.NotNull(storedAsset);
    }

    [Fact]
    public async Task ImageAssetAccessService_WithOwnedUploadedAsset_ReturnsAsset() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/owned.jpg", "https://cdn.example/owned.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        var service = new ImageAssetAccessService(repo, new FakeImageStorageService());

        var result = await service.ResolveOptionalAsync(asset.Id, owner, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(asset.Url, result.Value!.Url);
    }

    [Fact]
    public async Task ImageAssetAccessService_WithOtherOwner_ReturnsForbidden() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/owned.jpg", "https://cdn.example/owned.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        var service = new ImageAssetAccessService(repo, new FakeImageStorageService());

        var result = await service.ResolveOptionalAsync(asset.Id, UserId.New(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task ImageAssetAccessService_WithIncompleteUpload_ReturnsValidationFailure() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/pending.jpg", "https://cdn.example/pending.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        var service = new ImageAssetAccessService(
            repo,
            new FakeImageStorageService(new ImageObjectValidationResult(false, "not_found", "Image upload has not completed.")));

        var result = await service.ResolveOptionalAsync(asset.Id, owner, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("upload has not completed", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(true)
                : new DeleteImageAssetResult(false, errorCode));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeImageStorageService(
        ImageObjectValidationResult? validationResult = null) : IImageStorageService {
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

        public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
            string objectKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(validationResult ?? new ImageObjectValidationResult(true));
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingImageStorageService : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(
            UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated storage failure.");

        public Task<ImageObjectValidationResult> ValidateUploadedObjectAsync(
            string objectKey,
            CancellationToken cancellationToken) =>
            Task.FromException<ImageObjectValidationResult>(new InvalidOperationException("Simulated storage failure."));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeImageAssetRepository : IImageAssetRepository {
        private readonly Dictionary<ImageAssetId, ImageAsset> _assets = [];
        public HashSet<ImageAssetId> InUseIds { get; } = [];
        public int Count => _assets.Count;

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

        public Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
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
