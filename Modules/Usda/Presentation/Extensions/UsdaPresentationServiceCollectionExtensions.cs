using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Usda.Presentation.Extensions;

public static class UsdaPresentationServiceCollectionExtensions {
    public static IServiceCollection AddUsdaPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(UsdaPresentationServiceCollectionExtensions).Assembly);
    }
}
