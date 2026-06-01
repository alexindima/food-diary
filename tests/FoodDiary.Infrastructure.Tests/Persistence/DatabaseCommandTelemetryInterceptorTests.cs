using System.Diagnostics.Metrics;
using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure.Tests.Persistence;

public sealed class DatabaseCommandTelemetryInterceptorTests {
    private const string InfrastructureMeterName = "FoodDiary.Infrastructure";

    [Fact]
    public void RecordFailure_EmitsDatabaseFailureMetricWithExpectedTags() {
        long? failureCount = null;
        string? operation = null;
        string? source = null;
        string? errorType = null;
        using var listener = CreateInfrastructureListener((value, tags) => {
            failureCount = value;
            operation = GetTagValue(tags, "fooddiary.db.operation");
            source = GetTagValue(tags, "fooddiary.db.source");
            errorType = GetTagValue(tags, "error.type");
        });

        InfrastructureTelemetry.RecordDatabaseCommandFailure("reader", "LinqQuery", "PostgresException");

        Assert.Equal(1, failureCount);
        Assert.Equal("reader", operation);
        Assert.Equal("LinqQuery", source);
        Assert.Equal("PostgresException", errorType);
    }

    private static MeterListener CreateInfrastructureListener(
        Action<long, ReadOnlySpan<KeyValuePair<string, object?>>> onFailure) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (!string.Equals(instrument.Meter.Name, InfrastructureMeterName, StringComparison.Ordinal)) {
                return;
            }

            if (string.Equals(instrument.Name, "fooddiary.db.command.failures", StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => {
            if (string.Equals(instrument.Name, "fooddiary.db.command.failures", StringComparison.Ordinal)) {
                onFailure(value, tags);
            }
        });
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }
}
