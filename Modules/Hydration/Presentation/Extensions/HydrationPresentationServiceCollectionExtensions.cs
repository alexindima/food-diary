using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class HydrationPresentationServiceCollectionExtensions {
    public static IServiceCollection AddHydrationPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(HydrationPresentationServiceCollectionExtensions).Assembly);
    }
}
