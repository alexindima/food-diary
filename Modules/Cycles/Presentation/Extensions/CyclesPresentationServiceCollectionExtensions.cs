using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class CyclesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddCyclesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(CyclesPresentationServiceCollectionExtensions).Assembly);
    }
}
