using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Cycles.Presentation.Extensions;

public static class CyclesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddCyclesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(CyclesPresentationServiceCollectionExtensions).Assembly);
    }
}
