using FoodDiary.Modules.Billing.Application;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Infrastructure;

public static class ModuleRegistration {
    public static IServiceCollection AddBillingModule(this IServiceCollection services) {
        services.AddBillingApplication();
        services.AddScoped(provider => provider.GetRequiredService<IModuleContextFactory>()
            .CreateModuleContext<BillingDbContext>(options => new BillingDbContext(options)));
        services.AddScoped<BillingSubscriptionRepository>(static provider =>
            new BillingSubscriptionRepository(provider.GetRequiredService<BillingDbContext>().BillingSubscriptions,
                CreateTransactionSynchronizer(provider)));
        services.AddScoped<IBillingSubscriptionReadModelRepository>(static provider => provider.GetRequiredService<BillingSubscriptionRepository>());
        services.AddScoped<IBillingSubscriptionWriteRepository>(static provider => provider.GetRequiredService<BillingSubscriptionRepository>());
        services.AddScoped<BillingPaymentRepository>(static provider =>
            new BillingPaymentRepository(provider.GetRequiredService<BillingDbContext>().BillingPayments,
                CreateTransactionSynchronizer(provider)));
        services.AddScoped<IBillingPaymentReadRepository>(static provider => provider.GetRequiredService<BillingPaymentRepository>());
        services.AddScoped<IBillingPaymentWriteRepository>(static provider => provider.GetRequiredService<BillingPaymentRepository>());
        services.AddScoped<BillingWebhookEventRepository>(static provider =>
            new BillingWebhookEventRepository(
                provider.GetRequiredService<BillingDbContext>().BillingWebhookEvents,
                provider.GetService<TimeProvider>(), CreateTransactionSynchronizer(provider)));
        services.AddScoped<IBillingWebhookEventReadRepository>(static provider => provider.GetRequiredService<BillingWebhookEventRepository>());
        services.AddScoped<IBillingWebhookEventWriteRepository>(static provider => provider.GetRequiredService<BillingWebhookEventRepository>());
        services.AddScoped<IBillingTransactionRunner, EfBillingTransactionRunner>();
        services.AddScoped<IBillingCheckoutLock, PostgresBillingCheckoutLock>();
        return services;
    }

    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        BillingDbContext owned = provider.GetRequiredService<BillingDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(shared.Database.CurrentTransaction?.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
