using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class UsdaPresentationServiceCollectionExtensions {
    public static IServiceCollection AddUsdaPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(UsdaPresentationServiceCollectionExtensions).Assembly);
    }
}
