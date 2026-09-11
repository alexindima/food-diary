using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Presentation.Api.Hubs;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Builder;

namespace FoodDiary.Presentation.Api.Extensions;

public static class AiPresentationServiceCollectionExtensions {
    public static IServiceCollection AddAiPresentation(this IServiceCollection services) {
        services.AddHostedService<FoodRecognitionNotifier>();
        return services.AddPresentationAssembly(typeof(AiPresentationServiceCollectionExtensions).Assembly);
    }

    public static WebApplication MapAiPresentationHub(this WebApplication app, string corsPolicyName) {
        app.MapHub<FoodRecognitionHub>("/hubs/food-recognition",
            options => options.CloseOnAuthenticationExpiration = true).RequireCors(corsPolicyName);
        return app;
    }
}
