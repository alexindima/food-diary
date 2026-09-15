using FoodDiary.Modules.Products.Infrastructure.Services;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Products.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Products.Application;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Infrastructure.Persistence.Products;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Products.Infrastructure;

public static class ProductsModuleRegistration {
    public static IServiceCollection AddProductsModule(this IServiceCollection services) =>
        services.AddProductsApplication().AddProductsPersistence();

    public static IServiceCollection AddProductsPersistence(this IServiceCollection services) {
        services.AddMemoryCache();
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<ProductsDbContext>(options => new ProductsDbContext(options)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, ProductsUserDataPurgeParticipant>());
        services.AddScoped(provider => new ProductRepository(provider.GetRequiredService<ProductsDbContext>(),
            provider.GetRequiredService<IProductUsageQuery>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IProductSnapshotReadService>(static provider => new ProductSnapshotReadService(
            provider.GetRequiredService<ProductsDbContext>().Products, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<IProductReadRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductWriteRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductMutationTransactionRunner, EfProductMutationTransactionRunner>();
        services.AddScoped<IProductLookupService, ProductLookupService>();
        services.AddScoped<IFavoriteProductSourceReadService, FavoriteProductSourceReadService>();
        return services;
    }

    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
        ProductsDbContext owned = provider.GetRequiredService<ProductsDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
