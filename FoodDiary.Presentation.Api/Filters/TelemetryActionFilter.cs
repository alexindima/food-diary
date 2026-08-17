using System.Diagnostics;
using System.Globalization;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Filters;

public sealed class TelemetryActionFilter(ILogger<TelemetryActionFilter> logger) : IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        string controllerName = context.Controller.GetType().Name;
        string? actionName = context.ActionDescriptor.RouteValues.TryGetValue("action", out string? action) ? action : "Unknown";
        string feature = ResolveFeature(context.Controller.GetType());
        string operationName = $"{controllerName}.{actionName}";

        var stopwatch = Stopwatch.StartNew();
        Activity? activity = PresentationApiTelemetry.ActivitySource.StartActivity(operationName);
        activity?.SetTag("fooddiary.presentation.feature", feature);
        activity?.SetTag("fooddiary.presentation.controller", controllerName);
        activity?.SetTag("fooddiary.presentation.operation", operationName);

        ActionExecutedContext executedContext = await next();
        stopwatch.Stop();

        int statusCode = executedContext.HttpContext.Response.StatusCode;
        bool isSuccess = executedContext.Exception is null && statusCode < 400;
        string outcome = isSuccess ? "success" : "failure";

        CompleteActivity(activity, outcome, stopwatch.Elapsed.TotalMilliseconds, statusCode);

        RecordOperationMetrics(feature, controllerName, operationName, outcome, stopwatch.Elapsed.TotalMilliseconds);

        if (executedContext.Exception is not null) {
            activity?.SetStatus(ActivityStatusCode.Error);
            activity?.SetTag("error.type", executedContext.Exception.GetType().FullName);
            RecordFailureMetric(feature, controllerName, operationName, "UnhandledException");
        } else if (!isSuccess) {
            if (statusCode >= StatusCodes.Status500InternalServerError) {
                activity?.SetStatus(ActivityStatusCode.Error);
            }

            RecordFailureMetric(
                feature,
                controllerName,
                operationName,
                string.Create(CultureInfo.InvariantCulture, $"HttpStatus_{statusCode}"));
        }

        activity?.Dispose();

        if (!isSuccess) {
            logger.Log(
                ResolveFailureLogLevel(statusCode, executedContext.Exception),
                "Action {Operation} in {Feature}/{Controller} returned {StatusCode} in {ElapsedMs:F1}ms",
                operationName, feature, controllerName, statusCode, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static LogLevel ResolveFailureLogLevel(int statusCode, Exception? exception) =>
        exception is not null || statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Warning
            : LogLevel.Information;

    private static void RecordOperationMetrics(
        string feature,
        string controllerName,
        string operationName,
        string outcome,
        double durationMs) {
        KeyValuePair<string, object?>[] tags = [
            new("fooddiary.presentation.feature", feature),
            new("fooddiary.presentation.controller", controllerName),
            new("fooddiary.presentation.operation", operationName),
            new("fooddiary.presentation.outcome", outcome),
        ];
        PresentationApiTelemetry.OperationCounter.Add(1, tags);
        PresentationApiTelemetry.OperationDuration.Record(durationMs, tags);
    }

    private static void RecordFailureMetric(
        string feature,
        string controllerName,
        string operationName,
        string errorCode) {
        PresentationApiTelemetry.OperationFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", feature),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", controllerName),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", operationName),
            new KeyValuePair<string, object?>("error.code", errorCode));
    }

    private static void CompleteActivity(Activity? activity, string outcome, double durationMs, int statusCode) {
        activity?.SetTag("fooddiary.presentation.outcome", outcome);
        activity?.SetTag("fooddiary.presentation.duration_ms", durationMs);
        activity?.SetTag("http.response.status_code", statusCode);
    }

    private static string ResolveFeature(Type controllerType) {
        string? ns = controllerType.Namespace;
        if (string.IsNullOrWhiteSpace(ns)) {
            return "Unknown";
        }

        string[] segments = ns.Split('.');
        int idx = Array.IndexOf(segments, "Features");
        return idx >= 0 && idx < segments.Length - 1 ? segments[idx + 1] : "Unknown";
    }
}
