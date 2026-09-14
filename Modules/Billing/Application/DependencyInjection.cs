using System.Reflection;
using FluentValidation;
using FoodDiary.Modules.Billing.Application.Commands.ProcessBillingWebhook;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Application.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Billing.Application;

public static class DependencyInjection {
    public static IServiceCollection AddBillingApplication(this IServiceCollection services) {
        Assembly assembly = typeof(DependencyInjection).Assembly;
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<BillingAccessService>();
        services.AddScoped<BillingWebhookContextResolver>();
        services.AddScoped<BillingWebhookPaymentRecorder>();
        services.AddScoped<BillingWebhookPremiumRoleSyncer>();
        services.AddScoped<BillingWebhookSubscriptionWriter>();
        services.AddScoped<BillingWebhookEventProcessor>();
        services.AddScoped<IBillingWebhookInboxService, BillingWebhookInboxService>();
        return services;
    }
}
