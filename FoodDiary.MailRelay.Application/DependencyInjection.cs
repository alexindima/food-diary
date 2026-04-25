using FluentValidation;
using FoodDiary.MailRelay.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FoodDiary.MailRelay.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMailRelayApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(MailRelayLoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(MailRelayValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddSingleton<MailRelayDeliveryEventIngestionService>();
        services.AddSingleton<MailRelayEmailUseCases>();
        services.AddSingleton<SmtpSubmissionService>();
        services.AddSingleton<MailRelayMessageProcessor>();

        return services;
    }
}
