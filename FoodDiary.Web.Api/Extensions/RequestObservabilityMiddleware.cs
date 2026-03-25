using System.Diagnostics;
using System.Security.Claims;

namespace FoodDiary.Web.Api.Extensions;

public sealed class RequestObservabilityMiddleware(RequestDelegate next, ILogger<RequestObservabilityMiddleware> logger) {
    public async Task InvokeAsync(HttpContext context) {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        using var activity = ApiTelemetry.ActivitySource.StartActivity("fooddiary.http.request", ActivityKind.Internal);
        activity?.SetTag("http.request.method", request.Method);
        activity?.SetTag("url.path", request.Path.Value);
        activity?.SetTag("enduser.id", userId);

        using var scope = logger.BeginScope(new Dictionary<string, object?> {
            ["TraceId"] = context.TraceIdentifier,
            ["UserId"] = userId,
            ["RequestPath"] = request.Path.Value,
        });

        try {
            await next(context);
        } catch (Exception exception) {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.SetTag("error.type", exception.GetType().FullName);
            ApiTelemetry.RequestExceptionCounter.Add(
                1,
                new KeyValuePair<string, object?>("http.request.method", request.Method),
                new KeyValuePair<string, object?>("url.path", request.Path.Value));
            throw;
        } finally {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            activity?.SetTag("http.response.status_code", context.Response.StatusCode);
            ApiTelemetry.RequestCounter.Add(
                1,
                new KeyValuePair<string, object?>("http.request.method", request.Method),
                new KeyValuePair<string, object?>("url.path", request.Path.Value),
                new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            ApiTelemetry.RequestDuration.Record(
                elapsedMs,
                new KeyValuePair<string, object?>("http.request.method", request.Method),
                new KeyValuePair<string, object?>("url.path", request.Path.Value),
                new KeyValuePair<string, object?>("http.response.status_code", context.Response.StatusCode));
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                request.Method,
                request.Path.Value,
                context.Response.StatusCode,
                elapsedMs);
        }
    }
}
