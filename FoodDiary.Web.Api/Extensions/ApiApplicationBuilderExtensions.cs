using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiApplicationBuilderExtensions {
    extension(WebApplication app) {
        public WebApplication UseApiPipeline() {
            app.Services.GetService<TracerProvider>();
            app.Services.GetService<MeterProvider>();
            app.UseMiddleware<RequestObservabilityMiddleware>();
            app.UseExceptionHandler();
            app.UseForwardedHeaders();
            app.UseRequestLocalization(options => options
                .SetDefaultCulture("en")
                .AddSupportedCultures("en", "ru")
                .AddSupportedUICultures("en", "ru"));
            app.UseMiddleware<SecurityHeadersMiddleware>();
            app.UseHttpLogging();

            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            } else {
                app.UseHsts();
            }

            if (app.Services.GetRequiredService<IOptions<ApiHttpsRedirectionOptions>>().Value.Enabled) {
                app.UseHttpsRedirection();
            }

            app.UseCors(ApiCompositionConstants.CorsPolicyName);
            app.UseAuthentication();
            app.UseRateLimiter();
            app.UseAuthorization();
            app.UseMiddleware<ImpersonationAccessGuardMiddleware>();
            app.UseOutputCache();

            app.MapOperationalEndpoints();
            return app.MapPresentationApi(ApiCompositionConstants.CorsPolicyName);
        }

        private void MapOperationalEndpoints() {
            app.MapHealthChecks("/health/live", new HealthCheckOptions {
                Predicate = ExcludeHealthChecks,
            }).WithMetadata(new SuppressRequestAccessLogAttribute());
            app.MapHealthChecks("/health/ready", new HealthCheckOptions {
                Predicate = IsReadyHealthCheck,
            }).WithMetadata(new SuppressRequestAccessLogAttribute());
        }
    }

    private static bool ExcludeHealthChecks(HealthCheckRegistration _) => false;

    private static bool IsReadyHealthCheck(HealthCheckRegistration check) => check.Tags.Contains("ready");
}
