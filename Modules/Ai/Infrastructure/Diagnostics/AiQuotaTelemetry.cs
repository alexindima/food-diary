using System.Diagnostics.Metrics;

namespace FoodDiary.Modules.Ai.Infrastructure.Diagnostics;

internal static class AiQuotaTelemetry {
    internal const string MeterName = "FoodDiary.Infrastructure";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> OrphanCounter = Meter.CreateCounter<long>("fooddiary.ai.quota_orphans");

    internal static void RecordOrphans(int count) {
        if (count > 0) {
            OrphanCounter.Add(count);
        }
    }
}
