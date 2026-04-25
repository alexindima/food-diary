using FluentValidation;
using FoodDiary.MailInbox.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FoodDiary.MailInbox.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMailInboxApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration => {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(MailInboxLoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(MailInboxValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        return services;
    }
}
