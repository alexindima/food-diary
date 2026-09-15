using System.Diagnostics.Metrics;
using FoodDiary.Modules.Ai.Infrastructure.Diagnostics;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Diagnostics;

[ExcludeFromCodeCoverage]
public sealed class AiQuotaTelemetryTests {
    [Fact]
    public void RecordOrphans_PreservesExportedInstrumentAndIgnoresNonPositiveCounts() {
        var measurements = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, owner) => {
            if (string.Equals(instrument.Meter.Name, "FoodDiary.Infrastructure", StringComparison.Ordinal) &&
                string.Equals(instrument.Name, "fooddiary.ai.quota_orphans", StringComparison.Ordinal)) {
                owner.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) => {
            Assert.True(tags.IsEmpty);
            measurements.Add(value);
        });
        listener.Start();

        AiQuotaTelemetry.RecordOrphans(0);
        AiQuotaTelemetry.RecordOrphans(-1);
        AiQuotaTelemetry.RecordOrphans(3);

        Assert.Equal([3L], measurements);
    }
}
