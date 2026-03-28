using System.Diagnostics.Metrics;

namespace FoodDiary.JobManager.Services;

internal static class JobManagerTelemetry {
    public const string MeterName = "FoodDiary.JobManager";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> JobExecutionCounter = Meter.CreateCounter<long>(
        "fooddiary.job.execution.events");

    public static readonly Counter<long> JobDeletedItemsCounter = Meter.CreateCounter<long>(
        "fooddiary.job.deleted_items");

    public static readonly Histogram<double> JobExecutionDuration = Meter.CreateHistogram<double>(
        "fooddiary.job.execution.duration",
        unit: "ms");
}
