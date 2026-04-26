using System.Diagnostics.Metrics;

namespace FoodDiary.Application.Ai.Services;

internal static class ApplicationAiTelemetry {
    public const string MeterName = "FoodDiary.Application.Ai";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> QuotaRejectionCounter = Meter.CreateCounter<long>(
        "fooddiary.ai.quota_rejections");

    public static void RecordQuotaRejection(string operation) {
        QuotaRejectionCounter.Add(
            1,
            new KeyValuePair<string, object?>("fooddiary.ai.operation", operation));
    }
}
