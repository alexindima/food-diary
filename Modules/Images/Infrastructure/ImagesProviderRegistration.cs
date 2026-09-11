using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Images.Infrastructure;

public static class ImagesProviderRegistration {
    public static IServiceCollection AddImagesProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .Validate(S3Options.IsEmptyOrComplete,
                "S3 configuration must be empty or include AccessKeyId, SecretAccessKey, distinct Bucket/StagingBucket values, and Region or ServiceUrl.")
            .Validate(S3Options.HasValidMaxUploadSize,
                "S3:MaxUploadSizeBytes must be greater than zero and no greater than 50 MiB.")
            .Validate(S3Options.HasValidPublicBaseUrl,
                "S3:PublicBaseUrl must be an absolute HTTP or HTTPS URL when provided.")
            .Validate(S3Options.HasExplicitPublicImageAccessPolicy,
                "S3:AllowPublicImageAccess must be true for configured storage because image URLs are shared with users.")
            .Validate(S3Options.HasValidServiceUrl,
                "S3:ServiceUrl must be an absolute HTTP or HTTPS URL when provided.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IAmazonS3>(sp => {
            S3Options s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var credentials = new BasicAWSCredentials(s3Options.AccessKeyId, s3Options.SecretAccessKey);
            string? regionValue = s3Options.Region?.Trim();
            RegionEndpoint regionEndpoint = !string.IsNullOrWhiteSpace(regionValue)
                ? RegionEndpoint.GetBySystemName(regionValue)
                : RegionEndpoint.USEast1;
            var config = new AmazonS3Config {
                RegionEndpoint = regionEndpoint,
                AuthenticationRegion = regionEndpoint.SystemName,
                ForcePathStyle = !string.IsNullOrWhiteSpace(s3Options.ServiceUrl),
            };
            if (!string.IsNullOrWhiteSpace(s3Options.ServiceUrl)) {
                config.ServiceURL = s3Options.ServiceUrl;
            }

            return new AmazonS3Client(credentials, config);
        });
        services.AddSingleton<IObjectStorageClient, S3ObjectStorageClient>();
        services.AddSingleton<IImageStorageService>(sp => {
            IOptions<S3Options> options = sp.GetRequiredService<IOptions<S3Options>>();
            if (!S3Options.HasCompleteConfiguration(options.Value)) {
                return new UnconfiguredImageStorageService();
            }

            return new S3ImageStorageService(
                sp.GetRequiredService<IObjectStorageClient>(),
                options,
                sp.GetRequiredService<TimeProvider>());
        });
        return services;
    }
}
