using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Products;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class ProductsModuleRegistration {
    public static IServiceCollection AddProductsModule(this IServiceCollection services) =>
        services.AddProductsApplication().AddProductsPersistence();

    public static IServiceCollection AddProductsPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IUserDataPurgeParticipant, ProductsUserDataPurgeParticipant>());
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductSnapshotReadService, ProductSnapshotReadService>();
        services.AddScoped<IProductOverviewReadService, ProductOverviewReadService>();
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<IProductReadRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductWriteRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductMutationTransactionRunner, EfProductMutationTransactionRunner>();
        services.AddScoped<IProductLookupService, ProductLookupService>();
        services.AddScoped<IFavoriteProductSourceReadService, FavoriteProductSourceReadService>();
        return services;
    }
}
