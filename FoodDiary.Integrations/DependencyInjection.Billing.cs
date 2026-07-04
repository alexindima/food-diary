using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Integrations.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddBillingIntegrations(this IServiceCollection services) {
        services.AddSingleton<IBillingPublicConfigProvider, BillingPublicConfigProvider>();
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
