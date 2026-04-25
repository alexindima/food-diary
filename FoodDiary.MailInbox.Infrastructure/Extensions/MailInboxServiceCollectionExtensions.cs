using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Extensions;

public static class MailInboxServiceCollectionExtensions {
    public static IServiceCollection AddMailInboxInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddOptions<MailInboxSmtpOptions>()
            .Bind(configuration.GetSection(MailInboxSmtpOptions.SectionName))
            .Validate(MailInboxSmtpOptions.HasValidConfiguration, "MailInboxSmtp configuration is invalid.")
            .ValidateOnStart();

        services.AddSingleton(static sp => {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
            return NpgsqlDataSource.Create(connectionString);
        });

        services.AddSingleton<NpgsqlInboundMailStore>();
        services.AddSingleton<IInboundMailStore>(static sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxSchemaInitializer>(static sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxReadinessChecker, NpgsqlMailInboxReadinessChecker>();
        services.AddSingleton<SmtpInboundMessageStore>();
        services.AddSingleton<MailInboxMailboxFilter>();
        services.AddHostedService<MailInboxSchemaInitializerHostedService>();
        services.AddHostedService<MailInboxSmtpHostedService>();

        return services;
    }
}
