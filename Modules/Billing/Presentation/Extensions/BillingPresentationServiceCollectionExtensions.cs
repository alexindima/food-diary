using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Modules.Billing.Presentation.Security;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Presentation.Extensions;

public static class BillingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddBillingPresentation(this IServiceCollection services) {
        services.AddScoped<BillingWebhookHttpProcessor>();
        services.AddScoped<TelegramStarsBotAuthorizationFilter>();
        return services.AddPresentationAssembly(typeof(BillingPresentationServiceCollectionExtensions).Assembly);
    }
}
