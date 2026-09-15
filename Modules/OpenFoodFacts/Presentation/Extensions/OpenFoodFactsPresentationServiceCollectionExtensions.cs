using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.OpenFoodFacts.Presentation.Extensions;

public static class OpenFoodFactsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddOpenFoodFactsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(OpenFoodFactsPresentationServiceCollectionExtensions).Assembly);
    }
}
