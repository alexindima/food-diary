using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiApplicationBuilderExtensions {
    public static WebApplication UseApiPipeline(this WebApplication app) {
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
        app.UseOutputCache();

        app.MapHealthChecks("/health/live", new HealthCheckOptions {
            Predicate = _ => false,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions {
            Predicate = check => check.Tags.Contains("ready"),
        });
        app.MapPresentationApi(ApiCompositionConstants.CorsPolicyName);

        return app;
    }
}
