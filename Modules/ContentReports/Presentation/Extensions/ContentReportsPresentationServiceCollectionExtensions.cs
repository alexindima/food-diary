using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ContentReportsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddContentReportsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ContentReportsPresentationServiceCollectionExtensions).Assembly);
    }
}
