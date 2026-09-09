using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FoodDiary.Application.Runtime.Common.Services;

internal static class ModuleOperationTelemetry {
    private static readonly Meter Meter = new(ApplicationRuntimeTelemetry.MeterName);
    private static readonly Counter<long> Operations = Meter.CreateCounter<long>("fooddiary.module.operations");
    private static readonly UpDownCounter<long> Active = Meter.CreateUpDownCounter<long>("fooddiary.module.active");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "fooddiary.module.operation.duration", unit: "ms",
        advice: new InstrumentAdvice<double> {
            HistogramBucketBoundaries = [1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000, 30000],
        });

    public static void RecordActive(string module, string operation, long delta) =>
        Active.Add(delta,
            new KeyValuePair<string, object?>("module", module),
            new KeyValuePair<string, object?>("operation", operation));

    public static void RecordCompleted(string module, string operation, string outcome, TimeSpan duration) {
        TagList tags = new() { { "module", module }, { "operation", operation }, { "outcome", outcome } };
        Operations.Add(1, tags);
        // Outcomes belong on the counter only, avoiding a histogram per outcome.
        Duration.Record(duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("module", module),
            new KeyValuePair<string, object?>("operation", operation));
    }

    internal static string ResolveModule(string? assemblyName) {
        const string prefix = "FoodDiary.Application.";
        if (assemblyName?.StartsWith(prefix, StringComparison.Ordinal) == true) {
            string module = assemblyName[prefix.Length..];
            if (module.Length > 0 && !module.Contains('.', StringComparison.Ordinal) && module is not ("Tests" or "Runtime" or "Contracts")) {
                return module;
            }
        }

        return "Other";
    }
}
