using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Export.Presentation.Extensions;

public static class ExportPresentationServiceCollectionExtensions {
    public static IServiceCollection AddExportPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(ExportPresentationServiceCollectionExtensions).Assembly);
    }
}
