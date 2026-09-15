using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Products.Presentation.Extensions;

public static class ProductsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddProductsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ProductsPresentationServiceCollectionExtensions).Assembly);
    }
}
