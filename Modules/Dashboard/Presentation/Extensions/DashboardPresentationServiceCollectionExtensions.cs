using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Dashboard.Presentation.Extensions;

public static class DashboardPresentationServiceCollectionExtensions {
    public static IServiceCollection AddDashboardPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(DashboardPresentationServiceCollectionExtensions).Assembly);
    }
}
