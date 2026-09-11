using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Results;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class ImageAssetContentServiceTests {
    [Fact]
    public async Task GetDataUrlAsync_ReadsConfirmedObjectFromPublishedBucketInsteadOfLocalUrl() {
        var fixture = new Fixture();
        Result<string> result = await fixture.Service.GetDataUrlAsync(fixture.Asset.Id, fixture.Asset.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("data:image/png;base64,AQID", result.Value);
        Assert.Equal(("published", fixture.Asset.ObjectKey, 10L), fixture.Storage.Read);
        Assert.DoesNotContain("127.0.0.1", result.Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDataUrlAsync_RejectsForeignOrMissingAssetBeforeStorage(bool foreign) {
        var fixture = new Fixture();
        Result<string> result = await fixture.Service.GetDataUrlAsync(
            foreign ? fixture.Asset.Id : ImageAssetId.New(), foreign ? UserId.New() : fixture.Asset.UserId, CancellationToken.None);

        Assert.Equal("Image.NotFound", result.Error.Code);
        Assert.Equal(0, fixture.Storage.MetadataCalls);
    }

    [Fact]
    public async Task GetDataUrlAsync_RejectsUnconfirmedAssetBeforeStorage() {
        var fixture = new Fixture(confirmed: false);
        Result<string> result = await fixture.Service.GetDataUrlAsync(fixture.Asset.Id, fixture.Asset.UserId, CancellationToken.None);

        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Equal(0, fixture.Storage.MetadataCalls);
    }

    [Theory]
    [InlineData(0, "image/png")]
    [InlineData(11, "image/png")]
    [InlineData(3, "text/html")]
    [InlineData(3, null)]
    public async Task GetDataUrlAsync_RejectsInvalidMetadataWithoutDownloading(long size, string? type) {
        var fixture = new Fixture();
        fixture.Storage.Info = new StoredObjectInfo(size, type);
        Result<string> result = await fixture.Service.GetDataUrlAsync(fixture.Asset.Id, fixture.Asset.UserId, CancellationToken.None);

        Assert.Equal("Image.InvalidData", result.Error.Code);
        Assert.Null(fixture.Storage.Read);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDataUrlAsync_RejectsMissingOrChangedContent(bool missing) {
        var fixture = new Fixture();
        fixture.Storage.Content = missing ? null : [1];
        Result<string> result = await fixture.Service.GetDataUrlAsync(fixture.Asset.Id, fixture.Asset.UserId, CancellationToken.None);

        Assert.Equal("Image.InvalidData", result.Error.Code);
    }

    [Fact]
    public async Task GetDataUrlAsync_PropagatesCancellationDuringStorageRead() {
        var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        fixture.Storage.BeforeRead = cancellation.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fixture.Service.GetDataUrlAsync(
            fixture.Asset.Id, fixture.Asset.UserId, cancellation.Token));
    }

    [Theory]
    [InlineData(false, "Image.StorageError")]
    [InlineData(true, "Image.InvalidData")]
    public async Task GetDataUrlAsync_ReturnsSafeFailureForStorageErrors(bool oversized, string code) {
        var fixture = new Fixture();
        fixture.Storage.BeforeRead = () => {
            if (oversized) {
                throw new InvalidDataException("private object details");
            }
            throw new IOException("private object details");
        };
        Result<string> result = await fixture.Service.GetDataUrlAsync(fixture.Asset.Id, fixture.Asset.UserId, CancellationToken.None);

        Assert.Equal(code, result.Error.Code);
        Assert.DoesNotContain("private object details", result.Error.Message, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Fixture {
        public ImageAsset Asset { get; } = ImageAsset.Create(UserId.New(), "users/test/image.png", "http://127.0.0.1:9000/published/image.png");
        public Storage Storage { get; } = new();
        public ImageAssetContentService Service { get; }

        public Fixture(bool confirmed = true) {
            if (confirmed) {
                Asset.Confirm();
            }
            Service = new ImageAssetContentService(new Repository(Asset), Storage, Microsoft.Extensions.Options.Options.Create(new S3Options {
                Bucket = "published", StagingBucket = "staging", Region = "us-east-1",
                AccessKeyId = "test", SecretAccessKey = "test", MaxUploadSizeBytes = 10,
            }));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class Repository(ImageAsset asset) : IImageAssetReadRepository {
        public Task<ImageAsset?> GetOwnedByIdAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ImageAsset?>(id == asset.Id && userId == asset.UserId ? asset : null);
        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class Storage : IObjectStorageClient {
        public StoredObjectInfo Info { get; set; } = new(3, "image/png");
        public byte[]? Content { get; set; } = [1, 2, 3];
        public int MetadataCalls { get; private set; }
        public (string Bucket, string Key, long Maximum)? Read { get; private set; }
        public Action? BeforeRead { get; set; }
        public Task<StoredObjectInfo?> GetObjectInfoAsync(string bucketName, string key, CancellationToken cancellationToken) {
            MetadataCalls++;
            return Task.FromResult<StoredObjectInfo?>(Info);
        }
        public Task<byte[]?> GetObjectBytesAsync(string bucketName, string key, long maximumBytes, CancellationToken cancellationToken) {
            Read = (bucketName, key, maximumBytes);
            BeforeRead?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Content);
        }
        public string GetPreSignedUploadUrl(string bucketName, string key, string contentType, long contentLength, DateTime expiresAt) => throw new NotSupportedException();
        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task PutObjectBytesAsync(string bucketName, string key, string contentType, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
