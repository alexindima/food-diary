using System.Diagnostics;
using FoodDiary.MailRelay.Application.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FoodDiary.MailRelay.Presentation.Filters;

public sealed class MailRelayTelemetryActionFilter(ILogger<MailRelayTelemetryActionFilter> logger) : IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = context.HttpContext.GetEndpoint();
        var route = endpoint?.DisplayName ?? context.ActionDescriptor.AttributeRouteInfo?.Template ?? "unknown";
        var controllerName = context.Controller.GetType().Name;
        var feature = ResolveFeatureName(context.Controller.GetType());
        using var activity = MailRelayTelemetry.ActivitySource.StartActivity("MailRelay.Presentation", ActivityKind.Internal);

        activity?.SetTag("fooddiary.mailrelay.presentation.feature", feature);
        activity?.SetTag("fooddiary.mailrelay.presentation.controller", controllerName);
        activity?.SetTag("http.route", route);

        var executedContext = await next();
        stopwatch.Stop();

        var outcome = executedContext.Exception is null ? "success" : "failure";
        activity?.SetTag("fooddiary.mailrelay.presentation.outcome", outcome);
        activity?.SetTag("fooddiary.mailrelay.presentation.duration_ms", stopwatch.Elapsed.TotalMilliseconds);

        if (executedContext.Exception is not null) {
            activity?.SetStatus(ActivityStatusCode.Error, executedContext.Exception.Message);
            activity?.SetTag("error.type", executedContext.Exception.GetType().Name);
            logger.LogWarning(
                executedContext.Exception,
                "MailRelay presentation request {Route} in {Feature}/{Controller} failed in {ElapsedMs} ms",
                route,
                feature,
                controllerName,
                stopwatch.Elapsed.TotalMilliseconds);
        } else {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        MailRelayTelemetry.PresentationRequestCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.mailrelay.presentation.feature", feature),
            new KeyValuePair<string, object?>("fooddiary.mailrelay.presentation.controller", controllerName),
            new KeyValuePair<string, object?>("fooddiary.mailrelay.presentation.outcome", outcome));
        MailRelayTelemetry.PresentationRequestDuration.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("fooddiary.mailrelay.presentation.feature", feature),
            new KeyValuePair<string, object?>("fooddiary.mailrelay.presentation.controller", controllerName),
            new KeyValuePair<string, object?>("fooddiary.mailrelay.presentation.outcome", outcome));
    }

    private static string ResolveFeatureName(Type controllerType) {
        var namespaceValue = controllerType.Namespace;
        if (string.IsNullOrWhiteSpace(namespaceValue)) {
            return "Unknown";
        }

        var segments = namespaceValue.Split('.');
        var featuresIndex = Array.IndexOf(segments, "Features");
        return featuresIndex >= 0 && featuresIndex < segments.Length - 1
            ? segments[featuresIndex + 1]
            : "Unknown";
    }
}
