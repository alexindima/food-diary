using FoodDiary.Email.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Outbox.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Email.Infrastructure;

public static class EmailInfrastructureRegistration {
    public static IServiceCollection AddEmailInfrastructure(this IServiceCollection services) {
        services.AddLogging();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOutboxReplayStream, EmailOutboxReplayStream>());
        return services;
    }
}
