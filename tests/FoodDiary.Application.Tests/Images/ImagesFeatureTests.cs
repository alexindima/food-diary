using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
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
            CreateImageStorageService(),
            new FakeImageAssetRepository());

        var command = new GetImageUploadUrlCommand(Guid.Empty, "file.jpg", "image/jpeg", 100);
        Result<GetImageUploadUrlResult> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
    }

    [Theory]
    [InlineData("", "image/jpeg", 100, "File name")]
    [InlineData("photo.jpg", " ", 100, "Content type")]
    [InlineData("photo.jpg", "image/jpeg", 0, "File size")]
    public async Task GetImageUploadUrlCommandHandler_WithInvalidUploadMetadata_ReturnsFailure(
        string fileName,
        string contentType,
        long fileSizeBytes,
        string expectedMessage) {
        var handler = new GetImageUploadUrlCommandHandler(
            CreateImageStorageService(),
            new FakeImageAssetRepository());

        Result<GetImageUploadUrlResult> result = await handler.Handle(
            new GetImageUploadUrlCommand(Guid.NewGuid(), fileName, contentType, fileSizeBytes),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains(expectedMessage, result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetImageUploadUrlCommandHandler_WithValidRequest_PersistsAssetAndReturnsPresignedUrl() {
        var repository = new FakeImageAssetRepository();
        var handler = new GetImageUploadUrlCommandHandler(
            CreateImageStorageService(),
            repository);

        Result<GetImageUploadUrlResult> result = await handler.Handle(
            new GetImageUploadUrlCommand(Guid.NewGuid(), "photo.jpg", "image/jpeg", 1024),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("https://upload.example", result.Value.UploadUrl);
        Assert.Equal("https://cdn.example/file.jpg", result.Value.FileUrl);
        Assert.NotEqual(Guid.Empty, result.Value.AssetId);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WhenAssetMissing_ReturnsNotFound() {
        var handler = new DeleteImageAssetCommandHandler(
            new FakeImageAssetRepository(),
            CreateCleanupService());

        var assetId = Guid.NewGuid();
        Result result = await handler.Handle(new DeleteImageAssetCommand(Guid.NewGuid(), assetId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithOtherOwner_ReturnsForbidden() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var anotherUser = UserId.New();
        var asset = ImageAsset.Create(owner, "images/a.jpg", "https://cdn/a.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var handler = new DeleteImageAssetCommandHandler(repo, CreateCleanupService());
        Result result = await handler.Handle(new DeleteImageAssetCommand(anotherUser.Value, asset.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WhenCleanupReturnsStorageError_ReturnsStorageError() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/a.jpg", "https://cdn/a.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var handler = new DeleteImageAssetCommandHandler(repo, CreateCleanupService("storage_error"));
        Result result = await handler.Handle(new DeleteImageAssetCommand(owner.Value, asset.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.StorageError", result.Error.Code);
    }

    [Theory]
    [InlineData("invalid", "Image.InvalidData")]
    [InlineData("not_found", "Image.NotFound")]
    [InlineData("in_use", "Image.InUse")]
    [InlineData("storage_error", "Image.StorageError")]
    [InlineData("unexpected", "Image.InvalidData")]
    public async Task DeleteImageAssetCommandHandler_WhenCleanupFails_MapsErrorCode(string cleanupErrorCode, string expectedErrorCode) {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/a.jpg", "https://cdn/a.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var handler = new DeleteImageAssetCommandHandler(repo, CreateCleanupService(cleanupErrorCode));
        Result result = await handler.Handle(new DeleteImageAssetCommand(owner.Value, asset.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(expectedErrorCode, result.Error.Code);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithEmptyAssetId_ReturnsInvalidDataFailure() {
        var handler = new DeleteImageAssetCommandHandler(
            new FakeImageAssetRepository(),
            CreateCleanupService());

        Result result = await handler.Handle(new DeleteImageAssetCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("AssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteImageAssetCommandHandler_WithEmptyUserId_ReturnsInvalidDataFailure() {
        var handler = new DeleteImageAssetCommandHandler(
            new FakeImageAssetRepository(),
            CreateCleanupService());

        Result result = await handler.Handle(new DeleteImageAssetCommand(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImageAssetCleanupService_CleanupOrphans_WithNonPositiveBatch_ReturnsZero() {
        var service = new ImageAssetCleanupService(
            new FakeImageAssetRepository(),
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        int removed = await service.CleanupOrphansAsync(DateTime.UtcNow, 0, CancellationToken.None);

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
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        int removed = await service.CleanupOrphansAsync(
            DateTime.UtcNow.AddYears(1),
            10,
            CancellationToken.None);

        Assert.Equal(1, removed);
        Assert.Null(await repository.GetByIdAsync(removable.Id, CancellationToken.None));
        Assert.NotNull(await repository.GetByIdAsync(inUse.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ImageAssetCleanupService_CleanupOrphans_WithLocalCutoff_NormalizesToUtc() {
        var repository = new FakeImageAssetRepository();
        var service = new ImageAssetCleanupService(
            repository,
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());
        var localCutoff = new DateTime(2026, 5, 20, 12, 30, 0, DateTimeKind.Local);

        int removed = await service.CleanupOrphansAsync(localCutoff, 10, CancellationToken.None);

        Assert.Equal(0, removed);
        Assert.Equal(localCutoff.ToUniversalTime(), repository.LastUnusedOlderThanUtc);
        Assert.Equal(DateTimeKind.Utc, repository.LastUnusedOlderThanUtc!.Value.Kind);
    }

    [Fact]
    public async Task ImageAssetCleanupService_DeleteIfUnused_WhenInUse_ReturnsInUse() {
        var repo = new FakeImageAssetRepository();
        var asset = ImageAsset.Create(UserId.New(), "images/in-use.jpg", "https://cdn/in-use.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        repo.InUseIds.Add(asset.Id);

        var service = new ImageAssetCleanupService(
            repo,
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        DeleteImageAssetResult result = await service.DeleteIfUnusedAsync(asset.Id, CancellationToken.None);

        Assert.False(result.Deleted);
        Assert.Equal("in_use", result.ErrorCode);
    }

    [Fact]
    public async Task ImageAssetCleanupService_DeleteIfUnused_WithEmptyAssetId_ReturnsInvalid() {
        var service = new ImageAssetCleanupService(
            new FakeImageAssetRepository(),
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        DeleteImageAssetResult result = await service.DeleteIfUnusedAsync(ImageAssetId.Empty, CancellationToken.None);

        Assert.False(result.Deleted);
        Assert.Equal("invalid", result.ErrorCode);
    }

    [Fact]
    public async Task ImageAssetCleanupService_DeleteIfUnused_WhenAssetMissing_ReturnsNotFound() {
        var service = new ImageAssetCleanupService(
            new FakeImageAssetRepository(),
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        DeleteImageAssetResult result = await service.DeleteIfUnusedAsync(ImageAssetId.New(), CancellationToken.None);

        Assert.False(result.Deleted);
        Assert.Equal("not_found", result.ErrorCode);
    }

    [Fact]
    public async Task ImageAssetCleanupService_DeleteIfUnused_WhenDeleted_EnqueuesObjectDeletion() {
        var repo = new FakeImageAssetRepository();
        var outbox = new FakeImageObjectDeletionOutbox();
        var asset = ImageAsset.Create(UserId.New(), "images/removable.jpg", "https://cdn/removable.jpg");
        await repo.AddAsync(asset, CancellationToken.None);

        var service = new ImageAssetCleanupService(
            repo,
            outbox,
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        DeleteImageAssetResult result = await service.DeleteIfUnusedAsync(asset.Id, CancellationToken.None);
        ImageAsset? storedAsset = await repo.GetByIdAsync(asset.Id, CancellationToken.None);

        Assert.True(result.Deleted);
        Assert.Null(result.ErrorCode);
        Assert.Null(storedAsset);
        Assert.Equal(["images/removable.jpg"], outbox.ObjectKeys);
    }

    [Fact]
    public async Task ImageAssetCleanupService_DeleteIfUnused_WhenDeleted_DefersSaveToCaller() {
        var repo = new FakeImageAssetRepository();
        var asset = ImageAsset.Create(UserId.New(), "images/removable.jpg", "https://cdn/removable.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var service = new ImageAssetCleanupService(
            repo,
            new FakeImageObjectDeletionOutbox(),
            NullLogger<ImageAssetCleanupService>.Instance,
            unitOfWork);

        DeleteImageAssetResult result = await service.DeleteIfUnusedAsync(asset.Id, CancellationToken.None);

        Assert.True(result.Deleted);
        Assert.Null(await repo.GetByIdAsync(asset.Id, CancellationToken.None));
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImageAssetCleanupService_CleanupOrphans_WhenOneDeleteFails_ContinuesWithNextAsset() {
        var repo = new FakeImageAssetRepository();
        var failing = ImageAsset.Create(UserId.New(), "images/fail.jpg", "https://cdn/fail.jpg");
        var removable = ImageAsset.Create(UserId.New(), "images/removable.jpg", "https://cdn/removable.jpg");
        await repo.AddAsync(failing, CancellationToken.None);
        await repo.AddAsync(removable, CancellationToken.None);
        var service = new ImageAssetCleanupService(
            repo,
            new SelectivelyThrowingImageObjectDeletionOutbox("images/fail.jpg"),
            NullLogger<ImageAssetCleanupService>.Instance,
            CreateUnitOfWork());

        int removed = await service.CleanupOrphansAsync(
            DateTime.UtcNow.AddYears(1),
            10,
            CancellationToken.None);

        Assert.Equal(1, removed);
        Assert.NotNull(await repo.GetByIdAsync(failing.Id, CancellationToken.None));
        Assert.Null(await repo.GetByIdAsync(removable.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ImageAssetAccessService_WhenAssetMissing_ReturnsNotFound() {
        var service = new ImageAssetAccessService(new FakeImageAssetRepository(), CreateImageStorageService());
        var assetId = ImageAssetId.New();

        Result<ImageAsset?> result = await service.ResolveOptionalAsync(assetId, UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.NotFound", result.Error.Code);
        Assert.Contains(assetId.Value.ToString(), result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImageAssetAccessService_WithOwnedUploadedAsset_ReturnsAsset() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/owned.jpg", "https://cdn.example/owned.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        var service = new ImageAssetAccessService(repo, CreateImageStorageService());

        Result<ImageAsset?> result = await service.ResolveOptionalAsync(asset.Id, owner, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(asset.Url, result.Value!.Url);
    }

    [Fact]
    public async Task ImageAssetAccessService_WithOtherOwner_ReturnsForbidden() {
        var repo = new FakeImageAssetRepository();
        var owner = UserId.New();
        var asset = ImageAsset.Create(owner, "images/owned.jpg", "https://cdn.example/owned.jpg");
        await repo.AddAsync(asset, CancellationToken.None);
        var service = new ImageAssetAccessService(repo, CreateImageStorageService());

        Result<ImageAsset?> result = await service.ResolveOptionalAsync(asset.Id, UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
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
            CreateImageStorageService(new ImageObjectValidationResult(IsValid: false, "not_found", "Image upload has not completed.")));

        Result<ImageAsset?> result = await service.ResolveOptionalAsync(asset.Id, owner, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Contains("upload has not completed", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IImageAssetCleanupService CreateCleanupService(string? errorCode = null) {
        IImageAssetCleanupService service = Substitute.For<IImageAssetCleanupService>();
        service
            .DeleteIfUnusedAsync(Arg.Any<ImageAssetId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(Deleted: true)
                : new DeleteImageAssetResult(Deleted: false, errorCode)));
        service
            .CleanupOrphansAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return service;
    }

    private static IImageStorageService CreateImageStorageService(ImageObjectValidationResult? validationResult = null) {
        IImageStorageService service = Substitute.For<IImageStorageService>();
        service
            .CreatePresignedUploadAsync(
                Arg.Any<UserId>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<long>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PresignedUpload(
                "https://upload.example",
                "https://cdn.example/file.jpg",
                "images/file.jpg",
                DateTime.UtcNow.AddMinutes(10))));
        service
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        service
            .ValidateUploadedObjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(validationResult ?? new ImageObjectValidationResult(IsValid: true)));
        return service;
    }

    private static IImageStorageService CreateThrowingImageStorageService() {
        IImageStorageService service = Substitute.For<IImageStorageService>();
        var exception = new InvalidOperationException("Simulated storage failure.");
        service
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));
        service
            .ValidateUploadedObjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ImageObjectValidationResult>(exception));
        return service;
    }

    private static IImageStorageService CreateSelectivelyThrowingImageStorageService(string failingObjectKey) {
        IImageStorageService service = Substitute.For<IImageStorageService>();
        service
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                string objectKey = call.Arg<string>();
                return string.Equals(objectKey, failingObjectKey, StringComparison.Ordinal)
                    ? Task.FromException(new InvalidOperationException("Simulated storage failure."))
                    : Task.CompletedTask;
            });
        service
            .ValidateUploadedObjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ImageObjectValidationResult(IsValid: true)));
        return service;
    }

    private static IUnitOfWork CreateUnitOfWork() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return unitOfWork;
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeImageObjectDeletionOutbox : IImageObjectDeletionOutbox {
        public List<string> ObjectKeys { get; } = [];

        public Task EnqueueAsync(string objectKey, CancellationToken cancellationToken = default) {
            ObjectKeys.Add(objectKey);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class SelectivelyThrowingImageObjectDeletionOutbox(string failingObjectKey) : IImageObjectDeletionOutbox {
        public Task EnqueueAsync(string objectKey, CancellationToken cancellationToken = default) {
            if (string.Equals(objectKey, failingObjectKey, StringComparison.Ordinal)) {
                throw new InvalidOperationException("Object deletion outbox enqueue failed.");
            }

            return Task.CompletedTask;
        }
    }
    [ExcludeFromCodeCoverage]
    private sealed class FakeImageAssetRepository : IImageAssetRepository {
        private readonly Dictionary<ImageAssetId, ImageAsset> _assets = [];
        public HashSet<ImageAssetId> InUseIds { get; } = [];
        public int Count => _assets.Count;
        public DateTime? LastUnusedOlderThanUtc { get; private set; }

        public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
            _assets[asset.Id] = asset;
            return Task.FromResult(asset);
        }

        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) {
            _assets.TryGetValue(id, out ImageAsset? asset);
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
            LastUnusedOlderThanUtc = olderThanUtc;
            var result = _assets.Values
                .Where(a => a.CreatedOnUtc <= olderThanUtc && !InUseIds.Contains(a.Id))
                .Take(batchSize)
                .ToList();
            return Task.FromResult<IReadOnlyList<ImageAsset>>(result);
        }
    }
}
