using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.MailInbox.Initializer;

internal static class InitializerServiceCollectionExtensions {
    public static IServiceCollection AddMailInboxInitializerServices(
        this IServiceCollection services,
        string connectionString) {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddSingleton<DmarcReportParser>();
        services.AddSingleton<NpgsqlInboundMailStore>();
        services.AddSingleton<IMailInboxSchemaInitializer>(sp => sp.GetRequiredService<NpgsqlInboundMailStore>());

        return services;
    }
}
