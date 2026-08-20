using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Initializer.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.MailInbox.Initializer;

internal static class InitializerServiceCollectionExtensions {
    public static IServiceCollection AddMailInboxInitializerServices(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration) {
        services.AddOptions<MailInboxRuntimeDatabaseOptions>()
            .Bind(configuration.GetSection(MailInboxRuntimeDatabaseOptions.SectionName))
            .Validate(
                MailInboxRuntimeDatabaseOptions.HasValidConfiguration,
                "MailInbox runtime database role configuration is invalid.")
            .ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddOptions<MailInboxStorageOptions>();
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddSingleton<DmarcReportParser>();
        services.AddSingleton<IMailInboxDmarcReportParser>(sp => sp.GetRequiredService<DmarcReportParser>());
        services.AddSingleton<NpgsqlInboundMailStore>();
        services.AddSingleton<IMailInboxSchemaInitializer>(sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxReadinessChecker, NpgsqlMailInboxReadinessChecker>();
        services.AddSingleton<NpgsqlMailInboxRuntimeRoleProvisioner>();

        return services;
    }
}
