using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class GamificationPresentationServiceCollectionExtensions {
    public static IServiceCollection AddGamificationPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(GamificationPresentationServiceCollectionExtensions).Assembly);
    }
}
