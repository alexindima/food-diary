using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationApiTelemetry {
    public const string TelemetryName = "FoodDiary.Presentation.Api";

    public static readonly ActivitySource ActivitySource = new(TelemetryName);
    public static readonly Meter Meter = new(TelemetryName);
    public static readonly Counter<long> OperationCounter = Meter.CreateCounter<long>(
        "fooddiary.presentation.operations",
        unit: "{operation}",
        description: "Total number of completed presentation operations.");
    public static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        "fooddiary.presentation.operation.duration",
        unit: "ms",
        description: "Presentation operation duration in milliseconds.");
    public static readonly Counter<long> OperationFailureCounter = Meter.CreateCounter<long>(
        "fooddiary.presentation.operation.failures",
        unit: "{failure}",
        description: "Total number of failed presentation operations.");
}
