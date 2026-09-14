using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Extensions;

public static class BodyMetricsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddBodyMetricsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(BodyMetricsPresentationServiceCollectionExtensions).Assembly);
    }
}
