using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class TdeePresentationServiceCollectionExtensions {
    public static IServiceCollection AddTdeePresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(TdeePresentationServiceCollectionExtensions).Assembly);
    }
}
