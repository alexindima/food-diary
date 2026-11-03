using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using FoodDiary.Presentation.Api.Extensions;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Extensions;

[ExcludeFromCodeCoverage]
public static class ApiTelemetryServiceCollectionExtensions {
    extension(IServiceCollection services) {
        internal IServiceCollection AddConfiguredOpenTelemetry(IConfiguration configuration) {
            Uri? endpointUri = ResolveOtlpEndpoint(configuration);
            if (endpointUri is null) {
                return services;
            }

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("FoodDiary.Web.Api"))
                .WithTracing(tracing => tracing
                    .AddSource(ApiTelemetry.TelemetryName)
                    .AddSource(PresentationApiTelemetry.TelemetryName)
                    .AddAspNetCoreInstrumentation(options => {
                        options.Filter = TelemetryPrivacyProcessor.ShouldCollectRequest;
                        options.RecordException = false;
                        options.EnrichWithHttpResponse = TelemetryPrivacyProcessor.EnrichServerActivity;
                    })
                    .AddHttpClientInstrumentation(options => {
                        options.RecordException = false;
                        options.EnrichWithHttpRequestMessage = TelemetryPrivacyProcessor.EnrichClientActivity;
                        options.EnrichWithHttpResponseMessage = TelemetryPrivacyProcessor.EnrichClientActivity;
                    })
                    .AddNpgsql()
                    .AddProcessor(new TelemetryPrivacyProcessor())
                    .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                )
                .WithMetrics(metrics => metrics
                    .AddMeter(ApiTelemetry.TelemetryName)
                    .AddMeter(PresentationApiTelemetry.TelemetryName)
                    .AddMeter("FoodDiary.Application.Ai")
                    .AddMeter("FoodDiary.Application.Email")
                    .AddMeter("FoodDiary.Application.Runtime")
                    .AddMeter("FoodDiary.Persistence.ReadSnapshots")
                    .AddMeter("FoodDiary.Infrastructure")
                    .AddMeter("FoodDiary.Integrations")
                    .AddMeter("Npgsql")
                    // Excluded attributes can survive in exemplar filtered tags.
                    .SetExemplarFilter(ExemplarFilterType.AlwaysOff)
                    .AddView(CreateNpgsqlMetricView)
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                );

            return services;
        }
    }

    private static MetricStreamConfiguration? CreateNpgsqlMetricView(Instrument instrument) {
        if (!string.Equals(instrument.Meter.Name, "Npgsql", StringComparison.Ordinal)) {
            return null;
        }
        // A per-pool ratio cannot be aggregated after removing pool identity.
        if (string.Equals(instrument.Name, "db.client.operation.npgsql.prepared_ratio", StringComparison.Ordinal)) {
            return MetricStreamConfiguration.Drop;
        }
        string[] tagKeys = ["db.system.name", "db.client.connection.state"];
        return instrument is Histogram<double>
            ? new ExplicitBucketHistogramConfiguration {
                TagKeys = tagKeys,
                Boundaries = [0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30],
            }
            : new MetricStreamConfiguration { TagKeys = tagKeys };
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
