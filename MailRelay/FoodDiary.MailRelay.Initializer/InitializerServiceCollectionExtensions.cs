using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.MailRelay.Initializer;

internal static class InitializerServiceCollectionExtensions {
    public static IServiceCollection AddMailRelayInitializerServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString) {
        services.AddOptions<MailRelayQueueOptions>()
            .Bind(configuration.GetSection(MailRelayQueueOptions.SectionName))
            .Validate(MailRelayQueueOptions.HasValidConfiguration,
                "MailRelayQueue configuration requires positive poll interval, batch size, retry delays, and lock timeout.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddSingleton<MailRelayQueueStore>();
        services.AddSingleton<IMailRelaySchemaInitializer>(sp => sp.GetRequiredService<MailRelayQueueStore>());

        return services;
    }
}
