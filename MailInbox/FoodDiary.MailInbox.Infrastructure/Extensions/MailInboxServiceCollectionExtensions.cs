using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Application.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace FoodDiary.MailInbox.Infrastructure.Extensions;

public static class MailInboxServiceCollectionExtensions {
    public static IServiceCollection AddMailInboxInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddOptions<MailInboxSmtpOptions>()
            .Bind(configuration.GetSection(MailInboxSmtpOptions.SectionName))
            .Validate(MailInboxSmtpOptions.HasValidConfiguration, "MailInboxSmtp configuration is invalid.")
            .ValidateOnStart();

        services.AddOptions<OpenTelemetryOptions>()
            .Bind(configuration.GetSection(OpenTelemetryOptions.SectionName))
            .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint, "OpenTelemetry OTLP endpoint must be an absolute URI when configured.")
            .ValidateOnStart();

        services.AddSingleton(static sp => {
            string connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
            return NpgsqlDataSource.Create(connectionString);
        });

        services.AddSingleton<NpgsqlInboundMailStore>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DmarcReportParser>();
        services.AddSingleton<IInboundMailStore>(static sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxSchemaInitializer>(static sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxReadinessChecker, NpgsqlMailInboxReadinessChecker>();
        services.AddSingleton<SmtpInboundMessageStore>();
        services.AddSingleton<MailInboxMailboxFilter>();
        services.AddHostedService<MailInboxSchemaInitializerHostedService>();
        services.AddHostedService<MailInboxSmtpHostedService>();

        return services;
    }

    public static IServiceCollection AddMailInboxTelemetry(this IServiceCollection services) {
        services.AddSingleton<MeterProvider>(static serviceProvider => {
            OpenTelemetryOptions options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
            MeterProviderBuilder builder = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.MailInbox"))
                .AddMeter(MailInboxTelemetry.MeterName);

            if (!string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
                var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);
                builder.AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri);
            }

            return builder.Build();
        });

        return services;
    }
}
