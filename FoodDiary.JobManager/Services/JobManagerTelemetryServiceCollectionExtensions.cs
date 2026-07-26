using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

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

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("FoodDiary.JobManager"))
                .WithMetrics(metrics => metrics
                    .AddMeter(JobManagerTelemetry.MeterName)
                    .AddMeter("FoodDiary.Infrastructure")
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporter => exporter.Endpoint = endpointUri));

            return services;
        }
    }
}
