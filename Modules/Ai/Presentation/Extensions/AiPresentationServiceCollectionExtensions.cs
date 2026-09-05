using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class AiPresentationServiceCollectionExtensions {
    public static IServiceCollection AddAiPresentation(this IServiceCollection services) {
        return services.AddPresentationAssembly(typeof(AiPresentationServiceCollectionExtensions).Assembly);
    }
}
