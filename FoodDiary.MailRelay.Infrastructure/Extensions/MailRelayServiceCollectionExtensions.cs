using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace FoodDiary.MailRelay.Infrastructure.Extensions;

public static class MailRelayServiceCollectionExtensions {
    public static IServiceCollection AddMailRelayOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<MailRelayOptions>()
            .Bind(configuration.GetSection(MailRelayOptions.SectionName))
            .Validate(MailRelayOptions.HasValidListenApiKey, "MailRelay:ApiKey must be provided when RequireApiKey is enabled.")
            .ValidateOnStart();
        services.AddOptions<MailRelaySmtpOptions>()
            .Bind(configuration.GetSection(MailRelaySmtpOptions.SectionName))
            .Validate(static options => options.Port > 0, "RelaySmtp:Port must be greater than zero.")
            .ValidateOnStart();
        services.AddOptions<MailRelayDeliveryOptions>()
            .Bind(configuration.GetSection(MailRelayDeliveryOptions.SectionName))
            .Validate(MailRelayDeliveryOptions.HasSupportedMode,
                "MailRelayDelivery:Mode must be either SmtpSubmission or DirectMx.")
            .ValidateOnStart();
        services.AddOptions<DirectMxOptions>()
            .Bind(configuration.GetSection(DirectMxOptions.SectionName))
            .Validate(DirectMxOptions.HasValidConfiguration,
                "DirectMx configuration requires a positive port and connect timeout.")
            .ValidateOnStart();
        services.AddOptions<MailRelayDkimOptions>()
            .Bind(configuration.GetSection(MailRelayDkimOptions.SectionName))
            .Validate(MailRelayDkimOptions.HasValidConfiguration,
                "MailRelayDkim requires Domain, Selector, and exactly one of PrivateKeyPem or PrivateKeyPath when enabled.")
            .ValidateOnStart();
        services.AddOptions<MailRelayQueueOptions>()
            .Bind(configuration.GetSection(MailRelayQueueOptions.SectionName))
            .Validate(MailRelayQueueOptions.HasValidConfiguration,
                "MailRelayQueue configuration requires positive poll interval, batch size, retry delays, and lock timeout.")
            .ValidateOnStart();
        services.AddOptions<MailRelayBrokerOptions>()
            .Bind(configuration.GetSection(MailRelayBrokerOptions.SectionName))
            .Validate(MailRelayBrokerOptions.HasSupportedBackend,
                "MailRelayBroker:Backend must be either PostgresPolling or RabbitMq.")
            .Validate(MailRelayBrokerOptions.HasValidConfiguration,
                "MailRelayBroker configuration is invalid.")
            .ValidateOnStart();
        services.AddOptions<OpenTelemetryOptions>()
            .Bind(configuration.GetSection(OpenTelemetryOptions.SectionName))
            .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint,
                "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddMailRelayServices(this IServiceCollection services, IConfiguration configuration) {
        services.AddSingleton(_ => {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("DefaultConnection is not configured.");
            return new NpgsqlDataSourceBuilder(connectionString).Build();
        });

        services.AddSingleton<MailRelayQueueStore>();
        services.AddSingleton<IMailRelayQueueStore>(sp => sp.GetRequiredService<MailRelayQueueStore>());
        services.AddSingleton<IMailRelaySchemaInitializer>(sp => sp.GetRequiredService<MailRelayQueueStore>());
        services.AddSingleton<IMailRelayReadinessChecker, NpgsqlMailRelayReadinessChecker>();
        services.AddSingleton<DkimSigningService>();
        services.AddSingleton<SmtpRelayDeliveryTransport>();
        services.AddSingleton<DirectMxRelayDeliveryTransport>();
        services.AddSingleton<IMxResolver, DnsClientMxResolver>();
        services.AddSingleton<IRelayDeliveryTransport, ConfigurableRelayDeliveryTransport>();
        services.AddSingleton<RabbitMqMailRelayBroker>();
        services.AddSingleton<IMailRelayDispatchNotifier, RabbitMqMailRelayDispatchNotifier>();
        services.AddHostedService<MailRelaySchemaInitializerHostedService>();
        services.AddHostedService<RabbitMqMailRelayBootstrapHostedService>();
        services.AddHostedService<MailRelayOutboxPublisherHostedService>();
        services.AddHostedService<RabbitMqMailRelayConsumerHostedService>();
        services.AddHostedService<MailRelayQueueProcessorHostedService>();

        return services;
    }

    public static IServiceCollection AddMailRelayTelemetry(this IServiceCollection services) {
        services.AddSingleton<MeterProvider>(static serviceProvider => {
            var options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
                return null!;
            }

            var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);

            return Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.MailRelay"))
                .AddMeter(MailRelayTelemetry.MeterName)
                .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                .Build();
        });

        return services;
    }
}
