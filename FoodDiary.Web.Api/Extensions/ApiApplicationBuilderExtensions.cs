using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Web.Api.Build;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiApplicationBuilderExtensions {
    public static WebApplication UseApiPipeline(this WebApplication app) {
        app.Services.GetService<TracerProvider>();
        app.Services.GetService<MeterProvider>();
        app.UseExceptionHandler();
        app.UseForwardedHeaders();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseHttpLogging();
        app.UseMiddleware<RequestObservabilityMiddleware>();

        if (app.Environment.IsDevelopment()) {
            app.UseSwagger();
            app.UseSwaggerUI();
        } else {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(ApiCompositionConstants.CorsPolicyName);
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseMiddleware<ImpersonationAccessGuardMiddleware>();
        app.UseOutputCache();

        app.MapOperationalEndpoints();
        app.MapVersionEndpoints();
        app.MapPresentationApi(ApiCompositionConstants.CorsPolicyName);

        return app;
    }

    private static void MapOperationalEndpoints(this WebApplication app) {
        app.MapHealthChecks("/health/live", new HealthCheckOptions {
            Predicate = ExcludeHealthChecks,
        }).WithMetadata(new SuppressRequestAccessLogAttribute());
        app.MapHealthChecks("/health/ready", new HealthCheckOptions {
            Predicate = IsReadyHealthCheck,
        }).WithMetadata(new SuppressRequestAccessLogAttribute());
    }

    private static void MapVersionEndpoints(this WebApplication app) {
        static IResult BuildVersionResponse(ApiBuildInfo buildInfo) {
            return Results.Ok(new ApiVersionResponse(
                buildInfo.CommitSha,
                buildInfo.ImageTag,
                buildInfo.Environment,
                buildInfo.ApplicationVersion,
                buildInfo.StartedAtUtc));
        }

        app.MapGet("/api/version", BuildVersionResponse)
            .ExcludeFromDescription();
        app.MapGet("/api/v1/version", BuildVersionResponse)
            .ExcludeFromDescription();
    }

    private static bool ExcludeHealthChecks(HealthCheckRegistration _) => false;

    private static bool IsReadyHealthCheck(HealthCheckRegistration check) => check.Tags.Contains("ready");
}
