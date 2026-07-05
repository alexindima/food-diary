using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Integrations.Billing;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddBillingIntegrations(this IServiceCollection services) {
        services.AddSingleton<IBillingPublicConfigProvider, BillingPublicConfigProvider>();
        services.AddScoped<IStripeClient>(static sp => {
            StripeOptions options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
            string apiKey = string.IsNullOrWhiteSpace(options.SecretKey)
                ? "sk_not_configured"
                : options.SecretKey;
            return new StripeClient(apiKey);
        });
        services.AddScoped<IBillingProviderGateway, StripeBillingGateway>();
        services.AddHttpClient<PaddleBillingGateway>(client => {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IBillingProviderGateway>(sp => sp.GetRequiredService<PaddleBillingGateway>());
        services.AddHttpClient<YooKassaBillingGateway>(client => {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IBillingProviderGateway>(sp => sp.GetRequiredService<YooKassaBillingGateway>());
        services.AddScoped<IBillingRecurringProviderGateway>(sp => sp.GetRequiredService<YooKassaBillingGateway>());
        services.AddScoped<IBillingProviderGatewayAccessor, ConfigurableBillingProviderGatewayAccessor>();

        return services;
    }
}
