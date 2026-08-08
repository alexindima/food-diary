using System.Reflection;
using FluentValidation;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Billing;

public static class DependencyInjection {
    public static IServiceCollection AddBillingModule(this IServiceCollection services) {
        Assembly assembly = typeof(DependencyInjection).Assembly;
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<IBillingOverviewReadService, BillingOverviewReadService>();
        services.AddScoped<IBillingRenewalService, BillingRenewalService>();
        services.AddScoped<IBillingUserContextService, BillingUserContextService>();
        services.AddScoped<IBillingUserLookupService, BillingUserLookupService>();
        services.AddScoped<BillingAccessService>();
        services.AddScoped<BillingWebhookContextResolver>();
        services.AddScoped<BillingWebhookPaymentRecorder>();
        services.AddScoped<BillingWebhookPremiumRoleSyncer>();
        services.AddScoped<BillingWebhookSubscriptionWriter>();
        services.AddScoped<BillingWebhookEventProcessor>();
        services.AddScoped<IBillingWebhookInboxService, BillingWebhookInboxService>();
        services.AddScoped<BillingRenewalService>();
        return services;
    }
}
