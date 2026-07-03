using System.Net;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using FoodDiary.Integrations.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class S3ObjectStorageClientTests {
    [Fact]
    public void GetPreSignedUploadUrl_BuildsPutRequestAndReturnsUrl() {
        GetPreSignedUrlRequest? capturedRequest = null;
        IAmazonS3 amazonS3 = CreateS3Client((method, args) => {
            if (string.Equals(method.Name, nameof(IAmazonS3.GetPreSignedURL), StringComparison.Ordinal) &&
                args is [GetPreSignedUrlRequest request]) {
                capturedRequest = request;
                return "https://s3.example.com/upload";
            }

            throw new NotSupportedException(method.Name);
        });
        var client = new S3ObjectStorageClient(amazonS3);
        DateTime expiresAt = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);

        string url = client.GetPreSignedUploadUrl("bucket", "images/key.webp", "image/webp", expiresAt);

        Assert.Equal("https://s3.example.com/upload", url);
        Assert.NotNull(capturedRequest);
        Assert.Multiple(
            () => Assert.Equal("bucket", capturedRequest.BucketName),
            () => Assert.Equal("images/key.webp", capturedRequest.Key),
            () => Assert.Equal(HttpVerb.PUT, capturedRequest.Verb),
            () => Assert.Equal(expiresAt, capturedRequest.Expires),
            () => Assert.Equal("image/webp", capturedRequest.ContentType));
    }

    [Fact]
    public async Task DeleteObjectAsync_BuildsDeleteRequest() {
        DeleteObjectRequest? capturedRequest = null;
        IAmazonS3 amazonS3 = CreateS3Client((method, args) => {
            if (string.Equals(method.Name, nameof(IAmazonS3.DeleteObjectAsync), StringComparison.Ordinal) &&
                args is [DeleteObjectRequest request, CancellationToken]) {
                capturedRequest = request;
                return Task.FromResult(new DeleteObjectResponse());
            }

            throw new NotSupportedException(method.Name);
        });
        var client = new S3ObjectStorageClient(amazonS3);

        await client.DeleteObjectAsync("bucket", "images/key.webp", CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal("bucket", capturedRequest.BucketName);
        Assert.Equal("images/key.webp", capturedRequest.Key);
    }

    [Fact]
    public async Task GetObjectInfoAsync_WhenObjectExists_ReturnsSizeAndContentType() {
        IAmazonS3 amazonS3 = CreateS3Client((method, args) => {
            if (string.Equals(method.Name, nameof(IAmazonS3.GetObjectMetadataAsync), StringComparison.Ordinal) &&
                args is [GetObjectMetadataRequest request, CancellationToken]) {
                Assert.Equal("bucket", request.BucketName);
                Assert.Equal("images/key.webp", request.Key);
                var response = new GetObjectMetadataResponse {
                    ContentLength = 1234,
                };
                response.Headers.ContentType = "image/webp";
                return Task.FromResult(response);
            }

            throw new NotSupportedException(method.Name);
        });
        var client = new S3ObjectStorageClient(amazonS3);

        StoredObjectInfo? info = await client.GetObjectInfoAsync("bucket", "images/key.webp", CancellationToken.None);

        Assert.NotNull(info);
        Assert.Equal(1234, info.SizeBytes);
        Assert.Equal("image/webp", info.ContentType);
    }

    [Fact]
    public async Task GetObjectInfoAsync_WhenObjectIsMissing_ReturnsNull() {
        IAmazonS3 amazonS3 = CreateS3Client((method, args) => {
            if (string.Equals(method.Name, nameof(IAmazonS3.GetObjectMetadataAsync), StringComparison.Ordinal)) {
                throw new AmazonS3Exception("missing") {
                    StatusCode = HttpStatusCode.NotFound,
                };
            }

            throw new NotSupportedException(method.Name);
        });
        var client = new S3ObjectStorageClient(amazonS3);

        StoredObjectInfo? info = await client.GetObjectInfoAsync("bucket", "missing.webp", CancellationToken.None);

        Assert.Null(info);
    }

    private static IAmazonS3 CreateS3Client(Func<MethodInfo, object?[]?, object?> handler) {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, AmazonS3Proxy>();
        ((AmazonS3Proxy)(object)client).InvokeHandler = (method, args) => {
            if (string.Equals(method.Name, nameof(IDisposable.Dispose), StringComparison.Ordinal)) {
                return null;
            }

            return handler(method, args);
        };

        return client;
    }

    [ExcludeFromCodeCoverage]
    private class AmazonS3Proxy : DispatchProxy {
        public Func<MethodInfo, object?[]?, object?> InvokeHandler { get; set; } =
            (_, _) => throw new NotSupportedException();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return InvokeHandler(targetMethod, args);
        }
    }
}
