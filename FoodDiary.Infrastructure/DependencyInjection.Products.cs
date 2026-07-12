using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Infrastructure.Persistence.Products;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddProductsPersistence(this IServiceCollection services) {
        services.AddScoped<ProductRepository>();
        services.AddScoped<IProductOverviewReadService, ProductOverviewReadService>();
        services.AddScoped<IProductRepository, CachedProductRepository>();
        services.AddScoped<IProductReadRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductWriteRepository>(static provider => provider.GetRequiredService<IProductRepository>());
        services.AddScoped<IProductLookupService, ProductLookupService>();

        return services;
    }
}
