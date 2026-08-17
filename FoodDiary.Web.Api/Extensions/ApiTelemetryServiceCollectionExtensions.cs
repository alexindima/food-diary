using System.Diagnostics;
using FoodDiary.Presentation.Api.Extensions;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Extensions;

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
                    .AddMeter("FoodDiary.Infrastructure")
                    .AddMeter("FoodDiary.Integrations")
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                );

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
