using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FoodDiary.Web.Api.Extensions;

public static class ApiTelemetry {
    public const string TelemetryName = "FoodDiary.Web.Api";

    public static readonly ActivitySource ActivitySource = new(TelemetryName);
    public static readonly Meter Meter = new(TelemetryName);
    public static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "fooddiary.api.requests",
        unit: "{request}",
        description: "Total number of processed API requests.");
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "fooddiary.api.request.duration",
        unit: "ms",
        description: "API request duration in milliseconds.");
    public static readonly Counter<long> RequestExceptionCounter = Meter.CreateCounter<long>(
        "fooddiary.api.request.exceptions",
        unit: "{exception}",
        description: "Total number of unhandled API request exceptions.");
    public static readonly Counter<long> RateLimitRejectionCounter = Meter.CreateCounter<long>(
        "fooddiary.api.rate_limit.rejections",
        unit: "{rejection}",
        description: "Total number of API requests rejected by rate limiting.");
    public static readonly Counter<long> BusinessFlowCounter = Meter.CreateCounter<long>(
        "fooddiary.api.business_flow.events",
        unit: "{event}",
        description: "Total number of high-value backend business flow events by route group and outcome.");
}
