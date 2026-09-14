using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers;

public static partial class DependencyInjection {
    public static IServiceCollection AddBillingIntegrations(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddBillingIntegrationOptions(configuration);
        services.AddSingleton<IBillingPublicConfigProvider, BillingPublicConfigProvider>();
        services.AddScoped<IStripeClient>(static sp => {
            StripeOptions options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;
            string apiKey = string.IsNullOrWhiteSpace(options.SecretKey)
                ? "sk_not_configured"
                : options.SecretKey;
            return new StripeClient(apiKey);
        });
        services.AddScoped<IBillingProviderGateway, StripeBillingGateway>();
        services.AddHttpClient<PaddleBillingGateway>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();
        services.AddHttpClient<PaddleNotificationRecoveryService>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();
        services.AddScoped<IPaddleNotificationRecoveryGateway>(sp => sp.GetRequiredService<PaddleNotificationRecoveryService>());
        services.AddScoped<IBillingProviderGateway>(sp => sp.GetRequiredService<PaddleBillingGateway>());
        services.AddHttpClient<YooKassaBillingGateway>(client => client.Timeout = TimeSpan.FromSeconds(30))
            .RemoveAllLoggers();
        services.AddScoped<IBillingProviderGateway>(sp => sp.GetRequiredService<YooKassaBillingGateway>());
        services.AddScoped<IBillingRecurringProviderGateway>(sp => sp.GetRequiredService<YooKassaBillingGateway>());
        services.AddScoped<IBillingProviderGatewayAccessor, ConfigurableBillingProviderGatewayAccessor>();
        return services;
    }
}
