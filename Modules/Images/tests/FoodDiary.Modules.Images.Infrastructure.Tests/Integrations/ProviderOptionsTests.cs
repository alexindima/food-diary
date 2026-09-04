using FoodDiary.Integrations.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderOptionsTests {

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(50 * 1024 * 1024, true)]
    [InlineData((50 * 1024 * 1024) + 1, false)]
    public void S3Options_HasValidMaxUploadSize_RequiresBoundedPositiveValue(long maxUploadSizeBytes, bool expected) {
        var options = new S3Options { MaxUploadSizeBytes = maxUploadSizeBytes };

        Assert.Equal(expected, S3Options.HasValidMaxUploadSize(options));
    }

    [Fact]
    public void S3Options_IsEmptyOrComplete_AcceptsEmptyConfiguration() {
        var options = new S3Options();

        Assert.True(S3Options.IsEmptyOrComplete(options));
        Assert.False(S3Options.HasCompleteConfiguration(options));
    }

    [Theory]
    [InlineData("access", "secret", "bucket", "eu-central-1", null, true)]
    [InlineData("access", "secret", "bucket", "", "http://minio:9000", true)]
    [InlineData("access", "", "bucket", "eu-central-1", null, false)]
    [InlineData("access", "secret", "", "eu-central-1", null, false)]
    [InlineData("access", "secret", "bucket", "", null, false)]
    public void S3Options_IsEmptyOrComplete_RejectsPartialConfiguration(
        string accessKeyId,
        string secretAccessKey,
        string bucket,
        string region,
        string? serviceUrl,
        bool expected) {
        var options = new S3Options {
            AccessKeyId = accessKeyId,
            SecretAccessKey = secretAccessKey,
            Bucket = bucket,
            StagingBucket = string.IsNullOrWhiteSpace(bucket) ? string.Empty : $"{bucket}-staging",
            Region = region,
            ServiceUrl = serviceUrl,
        };

        Assert.Equal(expected, S3Options.IsEmptyOrComplete(options));
        Assert.Equal(expected, S3Options.HasCompleteConfiguration(options));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://cdn.example.com", true)]
    [InlineData("file:///tmp/assets", false)]
    [InlineData("not-a-url", false)]
    [InlineData("https://user:secret@cdn.example.com", false)]
    [InlineData("https://cdn.example.com?query=value", false)]
    public void S3Options_HasValidPublicBaseUrl_ValidatesAbsoluteUrl(string? publicBaseUrl, bool expected) {
        var options = new S3Options { PublicBaseUrl = publicBaseUrl };

        Assert.Equal(expected, S3Options.HasValidPublicBaseUrl(options));
    }

    [Fact]
    public void S3Options_HasExplicitPublicImageAccessPolicy_RequiresOptInForConfiguredStorage() {
        var configuredWithoutOptIn = new S3Options {
            AccessKeyId = "access",
            SecretAccessKey = "secret",
            Region = "eu-central-1",
            Bucket = "food-diary-test",
            StagingBucket = "food-diary-test-staging",
        };
        var configuredWithOptIn = new S3Options {
            AccessKeyId = "access",
            SecretAccessKey = "secret",
            Region = "eu-central-1",
            Bucket = "food-diary-test",
            StagingBucket = "food-diary-test-staging",
            AllowPublicImageAccess = true,
        };

        Assert.False(S3Options.HasExplicitPublicImageAccessPolicy(configuredWithoutOptIn));
        Assert.True(S3Options.HasExplicitPublicImageAccessPolicy(configuredWithOptIn));
        Assert.True(S3Options.HasExplicitPublicImageAccessPolicy(new S3Options()));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://s3.example.com", true)]
    [InlineData("http://minio:9000", false)]
    [InlineData("ftp://s3.example.com", false)]
    [InlineData("/relative", false)]
    [InlineData("http://user:secret@minio:9000", false)]
    [InlineData("http://minio:9000#fragment", false)]
    public void S3Options_HasValidServiceUrl_ValidatesAbsoluteUrl(string? serviceUrl, bool expected) {
        var options = new S3Options { ServiceUrl = serviceUrl };

        Assert.Equal(expected, S3Options.HasValidServiceUrl(options));
    }

    [Fact]
    public void S3Options_HasValidServiceUrl_AllowsExplicitInsecureHttpForDevelopment() {
        var options = new S3Options {
            ServiceUrl = "http://minio:9000",
            AllowInsecureHttp = true,
        };

        Assert.True(S3Options.HasValidServiceUrl(options));
    }
}
