using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class DashboardPresentationServiceCollectionExtensions {
    public static IServiceCollection AddDashboardPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(DashboardPresentationServiceCollectionExtensions).Assembly);
    }
}
