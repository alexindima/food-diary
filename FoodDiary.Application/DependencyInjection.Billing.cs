using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application;

public static partial class DependencyInjection {
    private static void AddBillingModule(this IServiceCollection services) {
        services.AddScoped<IBillingOverviewReadService, BillingOverviewReadService>();
        services.AddScoped<IBillingRenewalService, BillingRenewalService>();
        services.AddScoped<IBillingUserContextService, BillingUserContextService>();
        services.AddScoped<IBillingUserLookupService, BillingUserLookupService>();
        services.AddScoped<BillingAccessService>();
        services.AddScoped<BillingWebhookContextResolver>();
        services.AddScoped<BillingWebhookPaymentRecorder>();
        services.AddScoped<BillingWebhookPremiumRoleSyncer>();
        services.AddScoped<BillingWebhookSubscriptionWriter>();
        services.AddScoped<BillingRenewalService>();
    }
}
