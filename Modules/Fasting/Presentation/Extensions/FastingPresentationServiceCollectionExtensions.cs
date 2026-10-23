using FoodDiary.Modules.Fasting.Presentation.Features.Logs;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Fasting.Presentation.Extensions;

public static class FastingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddFastingPresentation(this IServiceCollection services) {
        services.AddScoped<ClientTelemetryHttpProcessor>();
        return services.AddPresentationAssembly(typeof(FastingPresentationServiceCollectionExtensions).Assembly);
    }
}
