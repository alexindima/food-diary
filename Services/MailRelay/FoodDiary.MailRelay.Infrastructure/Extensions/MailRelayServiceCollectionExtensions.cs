using System.Diagnostics;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodDiary.MailRelay.Infrastructure.Extensions;

public static class MailRelayServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddMailRelayOptions(IConfiguration configuration) {
            services.AddOptions<MailRelayOptions>()
                .Bind(configuration.GetSection(MailRelayOptions.SectionName))
                .Validate(MailRelayOptions.HasValidListenApiKey, "MailRelay:RequireApiKey must be true and MailRelay:ApiKey must be provided.")
                .Validate(MailRelayOptions.HasValidProviderWebhookConfiguration,
                    "MailRelay webhook verification requires MailgunWebhookSigningKey when Mailgun is enabled and ExpectedAwsSesSnsTopicArn when AWS SNS signature checks are enabled.")
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
                    "MailRelayBroker configuration is invalid. RabbitMq requires EnablePollingFallback=true so PostgreSQL remains a durable recovery path.")
                .ValidateOnStart();
            services.AddOptions<OpenTelemetryOptions>()
                .Bind(configuration.GetSection(OpenTelemetryOptions.SectionName))
                .Validate(OpenTelemetryOptions.HasValidOtlpEndpoint,
                    "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.")
                .ValidateOnStart();

            return services;
        }
        public IServiceCollection AddMailRelayServices(IConfiguration configuration) {
            services.AddSingleton(_ => {
                string connectionString = configuration.GetConnectionString("DefaultConnection")
                                          ?? throw new InvalidOperationException("DefaultConnection is not configured.");
                return new NpgsqlDataSourceBuilder(connectionString).Build();
            });

            services.AddSingleton<MailRelayQueueStore>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IMailRelayQueueStore>(sp => sp.GetRequiredService<MailRelayQueueStore>());
            services.AddSingleton<IMailRelaySchemaInitializer>(sp => sp.GetRequiredService<MailRelayQueueStore>());
            services.AddSingleton<IMailRelayReadinessChecker, MailRelayReadinessChecker>();
            services.AddSingleton<DkimSigningService>();
            services.AddSingleton<SmtpRelayDeliveryTransport>();
            services.AddSingleton<DirectMxRelayDeliveryTransport>();
            services.AddSingleton<IMailRelayDeliveryPolicy, ConfiguredMailRelayDeliveryPolicy>();
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
        public IServiceCollection AddMailRelayTelemetry(IConfiguration configuration) {
            Uri? endpointUri = ResolveOtlpEndpoint(configuration);
            if (endpointUri is null) {
                return services;
            }

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("FoodDiary.MailRelay"))
                .WithTracing(tracing => tracing
                    .AddSource(MailRelayTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation(options => {
                        options.Filter = MailRelayTelemetryPrivacyProcessor.ShouldCollectRequest;
                        options.RecordException = false;
                        options.EnrichWithHttpResponse = MailRelayTelemetryPrivacyProcessor.EnrichServerActivity;
                    })
                    .AddHttpClientInstrumentation(options => {
                        options.RecordException = false;
                        options.EnrichWithHttpRequestMessage = MailRelayTelemetryPrivacyProcessor.EnrichClientActivity;
                        options.EnrichWithHttpResponseMessage = MailRelayTelemetryPrivacyProcessor.EnrichClientActivity;
                    })
                    .AddNpgsql()
                    .AddProcessor(new MailRelayTelemetryPrivacyProcessor())
                    .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri))
                .WithMetrics(metrics => metrics
                    .AddMeter(MailRelayTelemetry.MeterName)
                    .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri));

            return services;
        }
    }

    private static Uri? ResolveOtlpEndpoint(IConfiguration configuration) {
        string? endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint)) {
            return null;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri)) {
            throw new InvalidOperationException(
                "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.");
        }

        return endpointUri;
    }
}
