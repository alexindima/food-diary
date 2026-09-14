using FoodDiary.Presentation.Api.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Presentation.Extensions;

public static class BillingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddBillingPresentation(this IServiceCollection services) {
        services.AddScoped<FoodDiary.Modules.Billing.Presentation.BillingWebhookHttpProcessor>();
        return services.AddPresentationAssembly(typeof(BillingPresentationServiceCollectionExtensions).Assembly);
    }
}
