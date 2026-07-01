using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiTelemetryServiceCollectionExtensions {
    extension(IServiceCollection services) {
        internal IServiceCollection AddConfiguredOpenTelemetry() {
            services.AddSingleton<TracerProvider>(static serviceProvider => {
                OpenTelemetryOptions options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
                    return null!;
                }

                var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);

                return Sdk.CreateTracerProviderBuilder()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.Web.Api"))
                    .AddSource(ApiTelemetry.TelemetryName)
                    .AddSource(PresentationApiTelemetry.TelemetryName)
                    .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                    .Build();
            });
            services.AddSingleton<MeterProvider>(static serviceProvider => {
                OpenTelemetryOptions options = serviceProvider.GetRequiredService<IOptions<OpenTelemetryOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.Otlp.Endpoint)) {
                    return null!;
                }

                var endpointUri = new Uri(options.Otlp.Endpoint, UriKind.Absolute);

                return Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("FoodDiary.Web.Api"))
                    .AddMeter(ApiTelemetry.TelemetryName)
                    .AddMeter(PresentationApiTelemetry.TelemetryName)
                    .AddMeter("FoodDiary.Application.Ai")
                    .AddMeter("FoodDiary.Application.Email")
                    .AddMeter("FoodDiary.Infrastructure")
                    .AddMeter("FoodDiary.Integrations")
                    .AddOtlpExporter(exporterOptions => exporterOptions.Endpoint = endpointUri)
                    .Build();
            });

            return services;
        }
    }
}
