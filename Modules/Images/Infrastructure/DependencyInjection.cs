using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodDiary.Modules.Images.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Infrastructure.Persistence.Images;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Images.Infrastructure;

public static class DependencyInjection {
    public static IServiceCollection AddImagesInfrastructure(this IServiceCollection services) {
        services.AddScoped(static provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<ImagesDbContext>(static options => new ImagesDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, ImagesUserDataPurgeParticipant>());
        services.AddScoped<IImageAssetOwnershipService, ImageAssetOwnershipService>();
        services.AddScoped<IImageAssetContentService, ImageAssetContentService>();
        services.AddScoped<IUserProfileImageService, UserProfileImageService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxReplayStream, ImageDeletionOutboxReplayStream>());
        services.AddScoped<IImageAssetCleanupBatch, ImageAssetCleanupBatch>();
        services.AddScoped<IImageAssetRepository>(static provider => new ImageAssetRepository(
            provider.GetRequiredService<ImagesDbContext>().ImageAssets, provider.GetRequiredService<IImageAssetUsageQuery>()));
        services.AddScoped<IImageAssetReadRepository>(static provider => provider.GetRequiredService<IImageAssetRepository>());
        services.AddScoped<IImageAssetWriteRepository>(static provider => provider.GetRequiredService<IImageAssetRepository>());
        services.AddScoped<IImageObjectDeletionOutbox>(static provider => new ImageObjectDeletionOutbox(
            provider.GetRequiredService<ImagesDbContext>().ImageObjectDeletionOutbox, provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<IImageObjectDeletionOutboxProcessor>(static provider => {
            ImagesDbContext owned = provider.GetRequiredService<ImagesDbContext>();
            IModuleScopeGuard scopeGuard = provider.GetRequiredService<IModuleScopeGuard>();
            return new ImageObjectDeletionOutboxProcessor(owned, owned.ImageObjectDeletionOutbox,
                provider.GetRequiredService<IImageStorageService>(), provider.GetRequiredService<IOptions<OutboxProcessingOptions>>(),
                provider.GetRequiredService<TimeProvider>(), provider.GetRequiredService<ILogger<ImageObjectDeletionOutboxProcessor>>(),
                scopeGuard.EnsureCleanEntry);
        });
        return services;
    }
}
