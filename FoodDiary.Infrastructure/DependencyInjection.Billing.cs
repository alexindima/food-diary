using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Infrastructure.Persistence.Billing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddBillingInfrastructure(this IServiceCollection services) {
        services.AddScoped<IBillingSubscriptionRepository, BillingSubscriptionRepository>();
        services.AddScoped<IBillingSubscriptionReadRepository>(static provider => provider.GetRequiredService<IBillingSubscriptionRepository>());
        services.AddScoped<IBillingSubscriptionReadModelRepository>(static provider => provider.GetRequiredService<IBillingSubscriptionRepository>());
        services.AddScoped<IBillingSubscriptionWriteRepository>(static provider => provider.GetRequiredService<IBillingSubscriptionRepository>());
        services.AddScoped<IBillingPaymentRepository, BillingPaymentRepository>();
        services.AddScoped<IBillingPaymentReadRepository>(static provider => provider.GetRequiredService<IBillingPaymentRepository>());
        services.AddScoped<IBillingPaymentWriteRepository>(static provider => provider.GetRequiredService<IBillingPaymentRepository>());
        services.AddScoped<IBillingWebhookEventRepository, BillingWebhookEventRepository>();
        services.AddScoped<IBillingWebhookEventReadRepository>(static provider => provider.GetRequiredService<IBillingWebhookEventRepository>());
        services.AddScoped<IBillingWebhookEventWriteRepository>(static provider => provider.GetRequiredService<IBillingWebhookEventRepository>());
        services.AddScoped<IBillingTransactionRunner, EfBillingTransactionRunner>();

        return services;
    }
}
