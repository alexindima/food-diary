using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.ContentReports.Presentation.Extensions;

public static class ContentReportsPresentationServiceCollectionExtensions {
    public static IServiceCollection AddContentReportsPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ContentReportsPresentationServiceCollectionExtensions).Assembly);
    }
}
