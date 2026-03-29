using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;

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
            var businessFlow = BusinessFlow.From(request.Method, request.Path);
            if (businessFlow is not null) {
                ApiTelemetry.BusinessFlowCounter.Add(
                    1,
                    new KeyValuePair<string, object?>("fooddiary.business_flow", businessFlow.Value.FlowName),
                    new KeyValuePair<string, object?>("fooddiary.business_outcome", ResolveOutcome(context.Response.StatusCode)),
                    new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            }
            var outputCacheObservation = OutputCacheObservation.From(context);
            if (outputCacheObservation is not null) {
                ApiTelemetry.OutputCacheCounter.Add(
                    1,
                    new KeyValuePair<string, object?>("fooddiary.output_cache.policy", outputCacheObservation.Value.PolicyName),
                    new KeyValuePair<string, object?>("fooddiary.output_cache.outcome", outputCacheObservation.Value.Outcome),
                    new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            }
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

    private static string ResolveOutcome(int statusCode) {
        return statusCode switch {
            >= 200 and < 400 => "success",
            >= 400 and < 500 => "client_error",
            _ => "server_error"
        };
    }

    private readonly record struct RequestSensitivity(string PathLabel, string ScopeLabel, bool IncludeUserIdInTelemetry) {
        public static RequestSensitivity From(PathString path) {
            if (path.StartsWithSegments("/api/v1/auth/admin-sso", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/api/v1/auth/admin-sso/*", "auth-admin-sso", false);
            }

            if (path.StartsWithSegments("/api/v1/auth/telegram", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/api/v1/auth/telegram/*", "auth-telegram", false);
            }

            if (path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/api/v1/auth/*", "auth", false);
            }

            if (path.StartsWithSegments("/hubs/email-verification", StringComparison.OrdinalIgnoreCase)) {
                return new RequestSensitivity("/hubs/email-verification", "signalr-auth", false);
            }

            return new RequestSensitivity(path.Value ?? "/", "standard", true);
        }
    }

    private readonly record struct BusinessFlow(string FlowName) {
        public static BusinessFlow? From(string method, PathString path) {
            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/register", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.register");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.login");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.refresh");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/restore", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.restore");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/password-reset/request", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.password-reset.request");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/password-reset/confirm", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.password-reset.confirm");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/verify-email", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.verify-email");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/auth/verify-email/resend", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("auth.verify-email.resend");
            }

            if (HttpMethods.IsPost(method) && path.Equals("/api/v1/images/upload-url", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("images.upload-url");
            }

            if (HttpMethods.IsDelete(method) && path.StartsWithSegments("/api/v1/images", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("images.delete");
            }

            if (HttpMethods.IsDelete(method) && path.Equals("/api/v1/users", StringComparison.OrdinalIgnoreCase)) {
                return new BusinessFlow("users.delete");
            }

            return null;
        }
    }

    private readonly record struct OutputCacheObservation(string PolicyName, string Outcome) {
        public static OutputCacheObservation? From(HttpContext context) {
            var endpoint = context.GetEndpoint();
            var outputCache = endpoint?.Metadata.GetMetadata<OutputCacheAttribute>();
            if (outputCache?.PolicyName is null) {
                return null;
            }

            var policyName = outputCache.PolicyName;
            var outcome = context.Response.Headers.ContainsKey("Age")
                ? "hit"
                : "miss";

            return new OutputCacheObservation(policyName, outcome);
        }
    }
}
