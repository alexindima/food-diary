using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Extensions;

public static class BillingPresentationServiceCollectionExtensions {
    public static IServiceCollection AddBillingPresentation(this IServiceCollection services) {
        services.AddScoped<FoodDiary.Presentation.Api.Features.Billing.BillingWebhookHttpProcessor>();
        return services.AddPresentationAssembly(typeof(BillingPresentationServiceCollectionExtensions).Assembly);
    }
}
