using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Gamification.Presentation.Extensions;

public static class GamificationPresentationServiceCollectionExtensions {
    public static IServiceCollection AddGamificationPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(GamificationPresentationServiceCollectionExtensions).Assembly);
    }
}
