using System.Diagnostics;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using FoodDiary.MailInbox.Application.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
            .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint, "OpenTelemetry OTLP endpoint must be an HTTP(S) URI without credentials, query, or fragment.")
            .ValidateOnStart();

        services.AddOptions<MailInboxStorageOptions>()
            .Bind(configuration.GetSection(MailInboxStorageOptions.SectionName))
            .Validate(MailInboxStorageOptions.HasValidConfiguration, "MailInboxStorage configuration is invalid.")
            .ValidateOnStart();

        services.AddSingleton(static sp => {
            string connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
            return NpgsqlDataSource.Create(connectionString);
        });

        services.AddSingleton<NpgsqlInboundMailStore>();
        services.AddSingleton<NpgsqlMailInboxRuntimeRoleValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DmarcReportParser>();
        services.AddSingleton<IMailInboxDmarcReportParser>(static sp => sp.GetRequiredService<DmarcReportParser>());
        services.AddSingleton<IInboundMailStore>(static sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxSchemaInitializer>(static sp => sp.GetRequiredService<NpgsqlInboundMailStore>());
        services.AddSingleton<IMailInboxReadinessChecker, NpgsqlMailInboxReadinessChecker>();
        services.AddSingleton<SmtpInboundMessageStore>();
        services.AddSingleton<MailInboxSlidingWindowRateLimiter>();
        services.AddSingleton<MailInboxMailboxFilter>();
        services.AddSingleton<MailInboxEndpointListenerFactory>();
        services.AddHostedService<MailInboxRetentionHostedService>();
        services.AddHostedService<MailInboxSmtpHostedService>();

        return services;
    }

    public static IServiceCollection AddMailInboxTelemetry(
        this IServiceCollection services,
        IConfiguration configuration) {
        Uri? endpointUri = ResolveOtlpEndpoint(configuration);
        if (endpointUri is null) {
            return services;
        }

        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("FoodDiary.MailInbox"))
            .WithTracing(tracing => tracing
                .AddSource(MailInboxTelemetry.MeterName)
                .AddAspNetCoreInstrumentation(options => {
                    options.Filter = MailInboxTelemetryPrivacyProcessor.ShouldCollectRequest;
                    options.RecordException = false;
                    options.EnrichWithHttpResponse = MailInboxTelemetryPrivacyProcessor.EnrichServerActivity;
                })
                .AddNpgsql()
                .AddProcessor(new MailInboxTelemetryPrivacyProcessor())
                .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri))
            .WithMetrics(metrics => metrics
                .AddMeter(MailInboxTelemetry.MeterName)
                .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri));

        return services;
    }

    private static Uri? ResolveOtlpEndpoint(IConfiguration configuration) {
        string? endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) {
            return null;
        }

        if (!OpenTelemetryOptions.TryCreateOtlpEndpoint(endpoint, out Uri? endpointUri)) {
            throw new InvalidOperationException(
                "OpenTelemetry:Otlp:Endpoint must be an HTTP(S) URI without credentials, query, or fragment.");
        }

        return endpointUri;
    }
}
