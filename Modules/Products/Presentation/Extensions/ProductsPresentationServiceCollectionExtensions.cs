using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ProductsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddProductsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ProductsPresentationServiceCollectionExtensions).Assembly);
    }
}
