using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class ExportPresentationServiceCollectionExtensions {
    public static IServiceCollection AddExportPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ExportPresentationServiceCollectionExtensions).Assembly);
    }
}
