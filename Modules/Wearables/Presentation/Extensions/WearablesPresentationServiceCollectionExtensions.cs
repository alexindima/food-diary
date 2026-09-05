using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class WearablesPresentationServiceCollectionExtensions {
    public static IServiceCollection AddWearablesPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(WearablesPresentationServiceCollectionExtensions).Assembly);
    }
}
