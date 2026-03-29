using Amazon.S3;
using Amazon.S3.Model;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.HealthChecks;

internal sealed class S3HealthCheck(IAmazonS3 s3Client, IOptions<S3Options> s3Options) : IHealthCheck {
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) {
        var bucket = s3Options.Value.Bucket;

        if (string.IsNullOrWhiteSpace(bucket)) {
            return HealthCheckResult.Healthy("S3 not configured.");
        }

        try {
            await s3Client.GetBucketLocationAsync(
                new GetBucketLocationRequest { BucketName = bucket },
                cancellationToken);
            return HealthCheckResult.Healthy();
        } catch (Exception ex) {
            return HealthCheckResult.Unhealthy($"S3 bucket unreachable: {bucket}", ex);
        }
    }
}
