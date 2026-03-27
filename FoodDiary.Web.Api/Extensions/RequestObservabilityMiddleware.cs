using System.Diagnostics;
using System.Security.Claims;

namespace FoodDiary.Web.Api.Extensions;

public sealed class RequestObservabilityMiddleware(RequestDelegate next, ILogger<RequestObservabilityMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var sensitivity = RequestSensitivity.From(request.Path);
        var userId = sensitivity.IncludeUserIdInTelemetry
            ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"
            : null;
        var pathLabel = sensitivity.PathLabel;
        using var activity = ApiTelemetry.ActivitySource.StartActivity("fooddiary.http.request", ActivityKind.Internal);
        activity?.SetTag("http.request.method", request.Method);
        activity?.SetTag("url.path", pathLabel);
        activity?.SetTag("fooddiary.request.sensitivity", sensitivity.ScopeLabel);
        if (userId is not null) {
            activity?.SetTag("enduser.id", userId);
        }

        using var scope = logger.BeginScope(new Dictionary<string, object?> {
            ["TraceId"] = context.TraceIdentifier,
            ["RequestPath"] = pathLabel,
            ["RequestSensitivity"] = sensitivity.ScopeLabel,
            ["UserId"] = userId,
        });

        try {
            await next(context);
        } catch (Exception exception) {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.SetTag("error.type", exception.GetType().FullName);
            ApiTelemetry.RequestExceptionCounter.Add(
                1,
                new KeyValuePair<string, object?>("http.request.method", request.Method),
                new KeyValuePair<string, object?>("url.path", pathLabel));
            throw;
        } finally {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            activity?.SetTag("http.response.status_code", context.Response.StatusCode);
            ApiTelemetry.RequestCounter.Add(
                1,
                new KeyValuePair<string, object?>("http.request.method", request.Method),
                new KeyValuePair<string, object?>("url.path", pathLabel),
                new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            ApiTelemetry.RequestDuration.Record(
                elapsedMs,
                new KeyValuePair<string, object?>("http.request.method", request.Method),
                new KeyValuePair<string, object?>("url.path", pathLabel),
                new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                request.Method,
                pathLabel,
                context.Response.StatusCode,
                elapsedMs);
        }
    }

    private readonly record struct RequestSensitivity(string PathLabel, string ScopeLabel, bool IncludeUserIdInTelemetry) {
        public static RequestSensitivity From(PathString path) {
            if (path.StartsWithSegments("/api/auth/admin-sso", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/api/auth/admin-sso/*", "auth-admin-sso", false);
            }

            if (path.StartsWithSegments("/api/auth/telegram", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/api/auth/telegram/*", "auth-telegram", false);
            }

            if (path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/api/auth/*", "auth", false);
            }

            if (path.StartsWithSegments("/hubs/email-verification", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/hubs/email-verification", "signalr-auth", false);
            }

            return new RequestSensitivity(path.Value ?? "/", "standard", true);
        }
    }
}
