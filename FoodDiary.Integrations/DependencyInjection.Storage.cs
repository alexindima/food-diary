using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Integrations.Options;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddStorageIntegrations(this IServiceCollection services) {
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
                ServiceURL = string.IsNullOrWhiteSpace(s3Options.ServiceUrl) ? null : s3Options.ServiceUrl,
                ForcePathStyle = !string.IsNullOrWhiteSpace(s3Options.ServiceUrl),
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddSingleton<IObjectStorageClient, S3ObjectStorageClient>();
        services.AddSingleton<IImageStorageService, S3ImageStorageService>();

        return services;
    }
}
