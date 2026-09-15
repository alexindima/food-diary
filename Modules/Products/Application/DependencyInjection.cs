using FoodDiary.Modules.Usda.Contracts.Common;
using FluentValidation;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Application.SearchSuggestions;
using FoodDiary.Modules.Products.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Products.Application;

public static class DependencyInjection {
    public static IServiceCollection AddProductsApplication(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddScoped<IProductSearchSuggestionProvider, OpenFoodFactsProductSearchSuggestionProvider>();
        services.AddScoped<IProductSearchSuggestionProvider, UsdaProductSearchSuggestionProvider>();
        services.AddScoped<RecentProductLoader>();
        services.AddScoped<IUsdaProductLinkService, ProductUsdaLinkService>();
        return services;
    }
}
