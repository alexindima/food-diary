using Amazon.S3;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Modules.Images.Infrastructure;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderRegistrationTests {
    [Fact]
    public async Task AddImagesProvider_EmptyConfigurationKeepsSafeFallbackAndDefaultClock() {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddImagesProvider(new ConfigurationBuilder().Build()));
        await using ServiceProvider provider = services.BuildServiceProvider();
        UnconfiguredImageStorageService storage = Assert.IsType<UnconfiguredImageStorageService>(provider.GetRequiredService<IImageStorageService>());
        ImageObjectValidationResult result = await storage.ConfirmUploadedObjectAsync("key", CancellationToken.None);
        Assert.Multiple(
            () => Assert.False(result.IsValid),
            () => Assert.Same(storage, provider.GetRequiredService<IImageStorageService>()),
            () => Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>()),
            () => Assert.Equal("FoodDiary.Modules.Images.Infrastructure", storage.GetType().Assembly.GetName().Name));
    }

    [Theory]
    [InlineData("eu-central-1", null, "eu-central-1", false)]
    [InlineData(" eu-central-1 ", "   ", "eu-central-1", false)]
    [InlineData("eu-central-1", "https://s3.example.com", "eu-central-1", true)]
    [InlineData("", "https://s3.example.com", "us-east-1", true)]
    public void AddImagesProvider_PreservesSingletonsEndpointRegionAndClock(string region, string? endpoint, string expectedRegion, bool pathStyle) {
        IConfiguration configuration = Configuration(region, endpoint, allowPublic: true);
        var services = new ServiceCollection();
        var clock = new FixedClock();
        services.AddSingleton<TimeProvider>(clock);
        services.AddImagesProvider(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IAmazonS3 client = provider.GetRequiredService<IAmazonS3>();
        AmazonS3Config config = Assert.IsType<AmazonS3Config>(client.Config);
        S3ImageStorageService storage = Assert.IsType<S3ImageStorageService>(provider.GetRequiredService<IImageStorageService>());
        S3ObjectStorageClient objectClient = Assert.IsType<S3ObjectStorageClient>(provider.GetRequiredService<IObjectStorageClient>());
        Assert.Multiple(
            () => Assert.Equal(expectedRegion, config.AuthenticationRegion),
            () => Assert.Equal(pathStyle, config.ForcePathStyle),
            () => Assert.Equal(pathStyle ? endpoint?.TrimEnd('/') : null, config.ServiceURL?.TrimEnd('/')),
            () => Assert.Equal(pathStyle ? null : expectedRegion, config.RegionEndpoint?.SystemName),
            () => Assert.Same(client, scope.ServiceProvider.GetRequiredService<IAmazonS3>()),
            () => Assert.Same(storage, scope.ServiceProvider.GetRequiredService<IImageStorageService>()),
            () => Assert.Same(objectClient, scope.ServiceProvider.GetRequiredService<IObjectStorageClient>()),
            () => Assert.Same(clock, provider.GetRequiredService<TimeProvider>()),
            () => Assert.Equal("FoodDiary.Modules.Images.Infrastructure", typeof(S3Options).Assembly.GetName().Name),
            () => Assert.Equal(typeof(S3Options).Assembly, typeof(S3ImageStorageService).Assembly));
    }

    [Fact]
    public void AddImagesProvider_ConfiguredStorageStillRequiresExplicitPublicAccess() {
        using ServiceProvider provider = new ServiceCollection().AddImagesProvider(Configuration("eu-central-1", endpoint: null, allowPublic: false)).BuildServiceProvider();
        OptionsValidationException error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<S3Options>>().Value);
        Assert.Equal(["S3:AllowPublicImageAccess must be true for configured storage because image URLs are shared with users."], error.Failures, StringComparer.Ordinal);
    }

    private static IConfiguration Configuration(string region, string? endpoint, bool allowPublic) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["S3:AccessKeyId"] = "test-access",
            ["S3:SecretAccessKey"] = "test-secret",
            ["S3:Bucket"] = "images",
            ["S3:StagingBucket"] = "staging",
            ["S3:Region"] = region,
            ["S3:ServiceUrl"] = endpoint,
            ["S3:AllowPublicImageAccess"] = allowPublic ? "true" : "false",
        }).Build();

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider;
}
