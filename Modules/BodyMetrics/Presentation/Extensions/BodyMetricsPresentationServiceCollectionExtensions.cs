using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class BodyMetricsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddBodyMetricsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(BodyMetricsPresentationServiceCollectionExtensions).Assembly);
    }
}
