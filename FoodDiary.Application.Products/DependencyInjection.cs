using FluentValidation;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Products.Products.Common;
using FoodDiary.Application.Products.Products.SearchSuggestions;
using FoodDiary.Application.Products.Products.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Products;

public static class DependencyInjection {
    public static IServiceCollection AddProductsModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<IRecentProductReadService, RecentProductReadService>();
        services.AddScoped<IProductUsdaLinkService, ProductUsdaLinkService>();
        return services;
    }
}
