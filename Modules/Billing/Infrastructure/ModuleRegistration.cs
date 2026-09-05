using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Billing;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddBillingModule(this IServiceCollection services) {
        services.AddBillingApplication();
        services.AddScoped<IBillingSubscriptionRepository>(static provider =>
            new BillingSubscriptionRepository(provider.GetRequiredService<FoodDiaryDbContext>().BillingSubscriptions));
        services.AddScoped<IBillingSubscriptionReadRepository>(static provider => provider.GetRequiredService<IBillingSubscriptionRepository>());
        services.AddScoped<IBillingSubscriptionReadModelRepository>(static provider => provider.GetRequiredService<IBillingSubscriptionRepository>());
        services.AddScoped<IBillingSubscriptionWriteRepository>(static provider => provider.GetRequiredService<IBillingSubscriptionRepository>());
        services.AddScoped<IBillingPaymentRepository>(static provider =>
            new BillingPaymentRepository(provider.GetRequiredService<FoodDiaryDbContext>().BillingPayments));
        services.AddScoped<IBillingPaymentReadRepository>(static provider => provider.GetRequiredService<IBillingPaymentRepository>());
        services.AddScoped<IBillingPaymentWriteRepository>(static provider => provider.GetRequiredService<IBillingPaymentRepository>());
        services.AddScoped<IBillingWebhookEventRepository>(static provider =>
            new BillingWebhookEventRepository(
                provider.GetRequiredService<FoodDiaryDbContext>().BillingWebhookEvents,
                provider.GetService<TimeProvider>()));
        services.AddScoped<IBillingWebhookEventReadRepository>(static provider => provider.GetRequiredService<IBillingWebhookEventRepository>());
        services.AddScoped<IBillingWebhookEventWriteRepository>(static provider => provider.GetRequiredService<IBillingWebhookEventRepository>());
        services.AddScoped<IBillingTransactionRunner, EfBillingTransactionRunner>();
        services.AddScoped<IBillingCheckoutLock, PostgresBillingCheckoutLock>();
        return services;
    }
}
