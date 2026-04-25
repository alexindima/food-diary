using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FoodDiary.MailRelay.Application;

public static class DependencyInjection {
    public static IServiceCollection AddMailRelayApplication(this IServiceCollection services) {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddSingleton<MailRelayDeliveryEventIngestionService>();
        services.AddSingleton<MailRelayEmailUseCases>();
        services.AddSingleton<SmtpSubmissionService>();
        services.AddSingleton<MailRelayMessageProcessor>();

        return services;
    }
}
