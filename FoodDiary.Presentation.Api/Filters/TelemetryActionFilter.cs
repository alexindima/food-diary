using System.Diagnostics;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Filters;

public class TelemetryActionFilter(ILogger<TelemetryActionFilter> logger) : IAsyncActionFilter {
    private const string StopwatchKey = "__TelemetryStopwatch";
    private const string ActivityKey = "__TelemetryActivity";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var controllerName = context.Controller.GetType().Name;
        var actionName = context.ActionDescriptor.RouteValues.TryGetValue("action", out var action) ? action : "Unknown";
        var feature = ResolveFeature(context.Controller.GetType());
        var operationName = $"{controllerName}.{actionName}";

        var stopwatch = Stopwatch.StartNew();
        var activity = PresentationApiTelemetry.ActivitySource.StartActivity(operationName, ActivityKind.Internal);
        activity?.SetTag("fooddiary.presentation.feature", feature);
        activity?.SetTag("fooddiary.presentation.controller", controllerName);
        activity?.SetTag("fooddiary.presentation.operation", operationName);

        var executedContext = await next();
        stopwatch.Stop();

        var statusCode = executedContext.HttpContext.Response.StatusCode;
        var isSuccess = executedContext.Exception is null && statusCode < 400;
        var outcome = isSuccess ? "success" : "failure";

        activity?.SetTag("fooddiary.presentation.outcome", outcome);
        activity?.SetTag("fooddiary.presentation.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
        activity?.SetTag("http.response.status_code", statusCode);

        PresentationApiTelemetry.OperationCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", feature),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", controllerName),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", operationName),
            new KeyValuePair<string, object?>("fooddiary.presentation.outcome", outcome));
        PresentationApiTelemetry.OperationDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("fooddiary.presentation.feature", feature),
            new KeyValuePair<string, object?>("fooddiary.presentation.controller", controllerName),
            new KeyValuePair<string, object?>("fooddiary.presentation.operation", operationName),
            new KeyValuePair<string, object?>("fooddiary.presentation.outcome", outcome));

        if (executedContext.Exception is not null) {
            activity?.SetStatus(ActivityStatusCode.Error, executedContext.Exception.Message);
            PresentationApiTelemetry.OperationFailureCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.presentation.feature", feature),
                new KeyValuePair<string, object?>("fooddiary.presentation.controller", controllerName),
                new KeyValuePair<string, object?>("fooddiary.presentation.operation", operationName),
                new KeyValuePair<string, object?>("error.code", "UnhandledException"));
        } else if (!isSuccess) {
            PresentationApiTelemetry.OperationFailureCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.presentation.feature", feature),
                new KeyValuePair<string, object?>("fooddiary.presentation.controller", controllerName),
                new KeyValuePair<string, object?>("fooddiary.presentation.operation", operationName),
                new KeyValuePair<string, object?>("error.code", $"HttpStatus_{statusCode}"));
        }

        activity?.Dispose();

        if (!isSuccess) {
            logger.LogWarning(
                "Action {Operation} in {Feature}/{Controller} returned {StatusCode} in {ElapsedMs:F1}ms",
                operationName, feature, controllerName, statusCode, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static string ResolveFeature(Type controllerType) {
        var ns = controllerType.Namespace;
        if (string.IsNullOrWhiteSpace(ns)) return "Unknown";
        var segments = ns.Split('.');
        var idx = Array.IndexOf(segments, "Features");
        return idx >= 0 && idx < segments.Length - 1 ? segments[idx + 1] : "Unknown";
    }
}
