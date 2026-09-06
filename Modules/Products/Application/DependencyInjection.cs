using FoodDiary.Application.Abstractions.Usda.Common;
using FluentValidation;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.SearchSuggestions;
using FoodDiary.Application.Products.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Products;

public static class DependencyInjection {
    public static IServiceCollection AddProductsApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<IRecentProductReadService, RecentProductReadService>();
        services.AddScoped<IUsdaProductLinkService, ProductUsdaLinkService>();
        return services;
    }
}
