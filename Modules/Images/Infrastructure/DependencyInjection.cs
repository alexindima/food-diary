using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Images.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddImagesInfrastructure(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxReplayStream, ImageDeletionOutboxReplayStream>());
        services.AddScoped<IImageAssetRepository, ImageAssetRepository>();
        services.AddScoped<IImageAssetReadRepository>(static provider => provider.GetRequiredService<IImageAssetRepository>());
        services.AddScoped<IImageAssetWriteRepository>(static provider => provider.GetRequiredService<IImageAssetRepository>());
        services.AddScoped<IImageObjectDeletionOutbox, ImageObjectDeletionOutbox>();
        services.AddScoped<IImageObjectDeletionOutboxProcessor, ImageObjectDeletionOutboxProcessor>();
        return services;
    }
}
