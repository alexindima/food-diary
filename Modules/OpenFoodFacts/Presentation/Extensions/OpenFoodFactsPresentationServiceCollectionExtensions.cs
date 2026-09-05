using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class OpenFoodFactsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddOpenFoodFactsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(OpenFoodFactsPresentationServiceCollectionExtensions).Assembly);
    }
}
