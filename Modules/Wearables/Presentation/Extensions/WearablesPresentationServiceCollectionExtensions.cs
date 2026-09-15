using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Wearables.Presentation.Extensions;

public static class WearablesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddWearablesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(WearablesPresentationServiceCollectionExtensions).Assembly);
    }
}
