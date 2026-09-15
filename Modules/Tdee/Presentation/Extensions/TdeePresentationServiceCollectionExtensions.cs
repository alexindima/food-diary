using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Tdee.Presentation.Extensions;

public static class TdeePresentationServiceCollectionExtensions {
    public static IServiceCollection AddTdeePresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(TdeePresentationServiceCollectionExtensions).Assembly);
    }
}
