using System.Diagnostics;
using System.Diagnostics.Metrics;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodDiary.JobManager.Services;

public static class JobManagerTelemetryServiceCollectionExtensions {
    extension(IServiceCollection services) {
        public IServiceCollection AddJobManagerOpenTelemetry(IConfiguration configuration) {
            string? endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];
            if (string.IsNullOrWhiteSpace(endpoint)) {
                return services;
            }

            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? endpointUri)) {
                throw new InvalidOperationException(
                    "OpenTelemetry:Otlp:Endpoint must be a valid absolute URI when provided.");
            }

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("FoodDiary.JobManager"))
                .WithTracing(tracing => tracing
                    .AddHttpClientInstrumentation(options => {
                        options.RecordException = false;
                        options.EnrichWithHttpRequestMessage = TelemetryPrivacyProcessor.EnrichClientActivity;
                        options.EnrichWithHttpResponseMessage = TelemetryPrivacyProcessor.EnrichClientActivity;
                    })
                    .AddNpgsql()
                    .AddProcessor(new TelemetryPrivacyProcessor())
                    .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri))
                .WithMetrics(metrics => metrics
                    .AddMeter(JobManagerTelemetry.MeterName)
                    .AddMeter("FoodDiary.Application.Runtime")
                    .AddMeter("FoodDiary.Persistence.ReadSnapshots")
                    .AddMeter("FoodDiary.Infrastructure")
                    .AddMeter("FoodDiary.Integrations")
                    .AddMeter("Npgsql")
                    // Excluded attributes can survive in exemplar filtered tags.
                    .SetExemplarFilter(ExemplarFilterType.AlwaysOff)
                    .AddView(CreateNpgsqlMetricView)
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri));

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
}
