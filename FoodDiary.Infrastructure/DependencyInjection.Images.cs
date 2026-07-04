using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddImagePersistence(this IServiceCollection services) {
        services.AddScoped<IImageAssetRepository, ImageAssetRepository>();
        services.AddScoped<IImageAssetReadRepository>(static provider => provider.GetRequiredService<IImageAssetRepository>());
        services.AddScoped<IImageAssetWriteRepository>(static provider => provider.GetRequiredService<IImageAssetRepository>());
        services.AddScoped<IImageObjectDeletionOutbox, ImageObjectDeletionOutbox>();
        services.AddScoped<IImageObjectDeletionOutboxProcessor, ImageObjectDeletionOutboxProcessor>();

        return services;
    }
}
