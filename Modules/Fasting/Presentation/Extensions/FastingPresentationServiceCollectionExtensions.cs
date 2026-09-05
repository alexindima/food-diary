using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class FastingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddFastingPresentation(this IServiceCollection services) {
        services.AddScoped<FoodDiary.Presentation.Api.Features.Logs.ClientTelemetryHttpProcessor>();
        return services.AddPresentationAssembly(typeof(FastingPresentationServiceCollectionExtensions).Assembly);
    }
}
