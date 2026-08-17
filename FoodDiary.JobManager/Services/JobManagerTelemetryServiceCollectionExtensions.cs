using System.Diagnostics;
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
                    .AddMeter("FoodDiary.Infrastructure")
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri));

            return services;
        }
    }
}
