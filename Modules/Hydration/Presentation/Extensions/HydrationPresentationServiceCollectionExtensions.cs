using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Hydration.Presentation.Extensions;

public static class HydrationPresentationServiceCollectionExtensions {
    public static IServiceCollection AddHydrationPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(HydrationPresentationServiceCollectionExtensions).Assembly);
    }
}
