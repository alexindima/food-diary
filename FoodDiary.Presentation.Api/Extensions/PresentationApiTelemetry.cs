using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FoodDiary.Presentation.Api.Extensions;

public static class PresentationApiTelemetry {
    public const string TelemetryName = "FoodDiary.Presentation.Api";

    public static readonly ActivitySource ActivitySource = new(TelemetryName);
    private static readonly Meter Meter = new(TelemetryName);
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
    public static readonly Counter<long> SecurityDecisionCounter = Meter.CreateCounter<long>(
        "fooddiary.presentation.security.decisions",
        unit: "{decision}",
        description: "Total number of presentation security decisions by operation and outcome.");
    public static readonly Histogram<double> IdempotencyActionDuration = Meter.CreateHistogram<double>(
        "fooddiary.idempotency.action.duration",
        unit: "ms",
        description: "Duration of actions protected by an acquired idempotency lease.");
    public static readonly Counter<long> IdempotencyLeaseLostCounter = Meter.CreateCounter<long>(
        "fooddiary.idempotency.lease.lost",
        unit: "{lease}",
        description: "Idempotency actions that could not prove lease ownership at finalization.");
    public static readonly Counter<long> IdempotencyLeaseRenewalFailureCounter = Meter.CreateCounter<long>(
        "fooddiary.idempotency.lease.renewal.failures",
        unit: "{failure}",
        description: "Failed idempotency lease renewal attempts by bounded reason.");
    public static readonly Counter<long> IdempotencyCompletionCasFailureCounter = Meter.CreateCounter<long>(
        "fooddiary.idempotency.completion.cas.failures",
        unit: "{failure}",
        description: "Completed actions whose idempotency response could not be persisted by the owning lease.");
}
