using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using FoodDiary.Integrations.Options;
using FoodDiary.Web.Api.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.IntegrationTests.HealthChecks;

[ExcludeFromCodeCoverage]
public sealed class S3HealthCheckTests {
    [Fact]
    public async Task CheckHealthAsync_WhenBucketIsBlank_ReturnsHealthyWithoutCallingS3() {
        var check = new S3HealthCheck(
            CreateS3Client((_, _) => throw new InvalidOperationException("S3 should not be called.")),
            OptionsFactory.Create(new S3Options { Bucket = string.Empty }));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("S3 not configured.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenBucketIsReachable_ReturnsHealthy() {
        GetBucketLocationRequest? capturedRequest = null;
        var check = new S3HealthCheck(
            CreateS3Client((request, _) => {
                capturedRequest = request;
                return Task.FromResult(new GetBucketLocationResponse());
            }),
            OptionsFactory.Create(new S3Options { Bucket = "food-diary-test" }));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(capturedRequest);
        Assert.Equal("food-diary-test", capturedRequest.BucketName);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenS3Throws_ReturnsUnhealthy() {
        var exception = new InvalidOperationException("S3 is down");
        var check = new S3HealthCheck(
            CreateS3Client((_, _) => throw exception),
            OptionsFactory.Create(new S3Options { Bucket = "food-diary-test" }));

        HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("S3 bucket unreachable: food-diary-test", result.Description);
        Assert.Same(exception, result.Exception);
    }

    private static IAmazonS3 CreateS3Client(
        Func<GetBucketLocationRequest, CancellationToken, Task<GetBucketLocationResponse>> handler) {
        IAmazonS3 client = DispatchProxy.Create<IAmazonS3, AmazonS3Proxy>();
        ((AmazonS3Proxy)(object)client).InvokeHandler = (method, args) => {
            if (string.Equals(method.Name, nameof(IAmazonS3.GetBucketLocationAsync), StringComparison.Ordinal) &&
                args is [GetBucketLocationRequest request, CancellationToken cancellationToken]) {
                return handler(request, cancellationToken);
            }

            if (string.Equals(method.Name, nameof(IDisposable.Dispose), StringComparison.Ordinal)) {
                return null;
            }

            throw new NotSupportedException(method.Name);
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
