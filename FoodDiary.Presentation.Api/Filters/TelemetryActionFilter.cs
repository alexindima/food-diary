using System.Diagnostics;
using System.Globalization;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Filters;

public sealed class TelemetryActionFilter(ILogger<TelemetryActionFilter> logger)
    : IAsyncResourceFilter, IAsyncAlwaysRunResultFilter, IOrderedFilter {
    private static readonly object ObservationKey = new();

    public int Order => int.MinValue;

    public async Task OnResourceExecutionAsync(
        ResourceExecutingContext context,
        ResourceExecutionDelegate next) {
        PresentationOperationObservation observation = BeginObservation(context);
        context.HttpContext.Items[ObservationKey] = observation;

        try {
            ResourceExecutedContext executedContext = await next().ConfigureAwait(false);
            Exception? exception = executedContext.ExceptionHandled ? null : executedContext.Exception;
            if (IsClientCancellation(executedContext.HttpContext, exception)) {
                CompleteObservation(observation, StatusCodes.Status499ClientClosedRequest, exception: null, isCancelled: true);
                return;
            }

            CompleteObservation(
                observation,
                ResolveStatusCode(
                    executedContext.HttpContext.Response.StatusCode,
                    exception),
                exception);
        } catch (OperationCanceledException exception) when (IsClientCancellation(context.HttpContext, exception)) {
            CompleteObservation(observation, StatusCodes.Status499ClientClosedRequest, exception: null, isCancelled: true);
            throw;
        } catch (Exception exception) {
            CompleteObservation(observation, StatusCodes.Status500InternalServerError, exception);
            throw;
        } finally {
            context.HttpContext.Items.Remove(ObservationKey);
        }
    }

    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next) {
        if (context.HttpContext.Items.ContainsKey(ObservationKey)) {
            await next().ConfigureAwait(false);
            return;
        }

        PresentationOperationObservation observation = BeginObservation(context);
        try {
            ResultExecutedContext executedContext = await next().ConfigureAwait(false);
            Exception? exception = executedContext.ExceptionHandled ? null : executedContext.Exception;
            if (IsClientCancellation(executedContext.HttpContext, exception)) {
                CompleteObservation(observation, StatusCodes.Status499ClientClosedRequest, exception: null, isCancelled: true);
                return;
            }

            CompleteObservation(
                observation,
                ResolveStatusCode(
                    executedContext.HttpContext.Response.StatusCode,
                    exception),
                exception);
        } catch (OperationCanceledException exception) when (IsClientCancellation(context.HttpContext, exception)) {
            CompleteObservation(observation, StatusCodes.Status499ClientClosedRequest, exception: null, isCancelled: true);
            throw;
        } catch (Exception exception) {
            CompleteObservation(observation, StatusCodes.Status500InternalServerError, exception);
            throw;
        }
    }

    private static PresentationOperationObservation BeginObservation(FilterContext context) {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        Type? controllerType = descriptor?.ControllerTypeInfo.AsType();
        string controllerName = descriptor?.ControllerName ?? "Unknown";
        string actionName = descriptor?.ActionName ??
                            (context.ActionDescriptor.RouteValues.TryGetValue("action", out string? action)
                                ? action ?? "Unknown"
                                : "Unknown");
        string feature = controllerType is null ? "Unknown" : ResolveFeature(controllerType);
        string controllerLabel = $"{controllerName}Controller";
        string operationName = $"{controllerLabel}.{actionName}";

        var stopwatch = Stopwatch.StartNew();
        Activity? activity = PresentationApiTelemetry.ActivitySource.StartActivity(operationName);
        activity?.SetTag("fooddiary.presentation.feature", feature);
        activity?.SetTag("fooddiary.presentation.controller", controllerLabel);
        activity?.SetTag("fooddiary.presentation.operation", operationName);

        return new PresentationOperationObservation(
            feature,
            controllerLabel,
            operationName,
            stopwatch,
            activity);
    }

    private void CompleteObservation(
        PresentationOperationObservation observation,
        int statusCode,
        Exception? exception,
        bool isCancelled = false) {
        observation.Stopwatch.Stop();
        bool isSuccess = exception is null && statusCode < StatusCodes.Status400BadRequest;
        string outcome = ResolveOutcome(isSuccess, isCancelled);

        CompleteActivity(
            observation.Activity,
            outcome,
            observation.Stopwatch.Elapsed.TotalMilliseconds,
            statusCode);
        RecordOperationMetrics(
            observation.Feature,
            observation.ControllerName,
            observation.OperationName,
            outcome,
            observation.Stopwatch.Elapsed.TotalMilliseconds);

        if (!isCancelled && exception is not null) {
            observation.Activity?.SetStatus(ActivityStatusCode.Error);
            observation.Activity?.SetTag("error.type", exception.GetType().FullName);
            RecordFailureMetric(
                observation.Feature,
                observation.ControllerName,
                observation.OperationName,
                "UnhandledException");
        } else if (!isCancelled && !isSuccess) {
            if (statusCode >= StatusCodes.Status500InternalServerError) {
                observation.Activity?.SetStatus(ActivityStatusCode.Error);
            }

            RecordFailureMetric(
                observation.Feature,
                observation.ControllerName,
                observation.OperationName,
                string.Create(CultureInfo.InvariantCulture, $"HttpStatus_{statusCode}"));
        }

        observation.Activity?.Dispose();

        if (!isSuccess && !isCancelled) {
            logger.Log(
                ResolveFailureLogLevel(statusCode, exception),
                "Action {Operation} in {Feature}/{Controller} returned {StatusCode} in {ElapsedMs:F1}ms",
                observation.OperationName,
                observation.Feature,
                observation.ControllerName,
                statusCode,
                observation.Stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static int ResolveStatusCode(int responseStatusCode, Exception? exception) =>
        exception is not null
            ? StatusCodes.Status500InternalServerError
            : responseStatusCode;

    private static bool IsClientCancellation(HttpContext context, Exception? exception) {
        if (exception is not OperationCanceledException cancellationException) {
            return false;
        }

        CancellationToken requestCancellation = context.RequestAborted;
        return requestCancellation.IsCancellationRequested ||
               (requestCancellation.CanBeCanceled && cancellationException.CancellationToken == requestCancellation);
    }

    private static string ResolveOutcome(bool isSuccess, bool isCancelled) {
        if (isCancelled) {
            return "cancelled";
        }

        return isSuccess ? "success" : "failure";
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

    private sealed record PresentationOperationObservation(
        string Feature,
        string ControllerName,
        string OperationName,
        Stopwatch Stopwatch,
        Activity? Activity);
}
