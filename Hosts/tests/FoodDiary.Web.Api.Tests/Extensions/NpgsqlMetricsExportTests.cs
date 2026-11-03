using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using FoodDiary.Web.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class NpgsqlMetricsExportTests {
    [Fact]
    public void Host_ExportsDatabaseMetricsWithoutPrivateAttributesOrExemplars() {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["OpenTelemetry:Otlp:Endpoint"] = "http://localhost:4317",
            }).Build();
        // A host's explicit privacy policy must override an exemplar-enabled default.
        services.AddOpenTelemetry().WithMetrics(metrics => metrics.SetExemplarFilter(ExemplarFilterType.AlwaysOn));
        services.AddConfiguredOpenTelemetry(configuration);
        var exporter = new RecordingExporter();
        var reader = new PeriodicExportingMetricReader(exporter, Timeout.Infinite, 1_000);
        services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddReader(reader));
        using ServiceProvider provider = services.BuildServiceProvider();
        MeterProvider meterProvider = provider.GetRequiredService<MeterProvider>();
        using var meter = new Meter("Npgsql", "fooddiary-export-test");
        Histogram<double> duration = meter.CreateHistogram<double>("db.client.operation.duration", "s",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1, 5, 10] });
        UpDownCounter<long> connections = meter.CreateUpDownCounter<long>("db.client.connection.count", "{connection}");
        UpDownCounter<long> maximum = meter.CreateUpDownCounter<long>("db.client.connection.max", "{connection}");
        UpDownCounter<long> pending = meter.CreateUpDownCounter<long>("db.client.connection.npgsql.pending_requests", "{request}");
        Counter<long> timeouts = meter.CreateCounter<long>("db.client.connection.npgsql.timeouts", "{timeout}");
        meter.CreateObservableGauge("db.client.operation.npgsql.prepared_ratio", () => 50d);
        duration.Record(0.025, PrivateTags("pool-a"));
        duration.Record(0.125, PrivateTags("pool-b"));
        connections.Add(2, PrivateTags("pool-a", "used"));
        connections.Add(3, PrivateTags("pool-b", "idle"));
        maximum.Add(20, PrivateTags("pool-a"));
        pending.Add(1, PrivateTags("pool-a"));
        timeouts.Add(1, PrivateTags("pool-a"));

        Assert.True(reader.Collect(5_000));
        Sample[] durations = [.. exporter.Samples.Where(sample => string.Equals(sample.Name, "db.client.operation.duration", StringComparison.Ordinal))];
        Assert.True(durations.Length > 0, "SQL duration was not exported");
        Sample operation = Assert.Single(durations);
        Assert.Multiple(
            () => Assert.Equal("s", operation.Unit),
            () => Assert.Equal(2, operation.Count),
            () => Assert.Equal(0.15, operation.Value, precision: 8),
            () => Assert.Contains(0.001, operation.Bounds),
            () => Assert.Contains(0.05, operation.Bounds),
            () => Assert.Contains(30, operation.Bounds),
            () => Assert.Contains(exporter.Samples, sample => string.Equals(sample.Name, "db.client.connection.count", StringComparison.Ordinal) &&
                string.Equals(sample.Tags.GetValueOrDefault("db.client.connection.state"), "used", StringComparison.Ordinal) && sample.Value == 2),
            () => Assert.Contains(exporter.Samples, sample => string.Equals(sample.Name, "db.client.connection.count", StringComparison.Ordinal) &&
                string.Equals(sample.Tags.GetValueOrDefault("db.client.connection.state"), "idle", StringComparison.Ordinal) && sample.Value == 3),
            () => Assert.Contains(exporter.Samples, sample => string.Equals(sample.Name, "db.client.connection.max", StringComparison.Ordinal) && sample.Value == 20),
            () => Assert.Contains(exporter.Samples, sample => string.Equals(sample.Name, "db.client.connection.npgsql.pending_requests", StringComparison.Ordinal) && sample.Value == 1),
            () => Assert.Contains(exporter.Samples, sample => string.Equals(sample.Name, "db.client.connection.npgsql.timeouts", StringComparison.Ordinal) && sample.Value == 1),
            () => Assert.DoesNotContain(exporter.Samples, sample => string.Equals(sample.Name, "db.client.operation.npgsql.prepared_ratio", StringComparison.Ordinal)));
        Assert.All(exporter.Samples, sample => {
            Assert.False(sample.HasExemplars);
            Assert.All(sample.Tags.Keys, key => Assert.Contains(key, ["db.system.name", "db.client.connection.state"], StringComparer.Ordinal));
            Assert.DoesNotContain(sample.Tags.Values, value => value.Contains("private", StringComparison.Ordinal));
        });
    }

    private static KeyValuePair<string, object?>[] PrivateTags(string pool, string? state = null) {
        var tags = new List<KeyValuePair<string, object?>> {
            new("db.system.name", "postgresql"),
            new("db.client.connection.pool.name", $"Host=private-host;Username=private-user;Password=private-password;Application Name={pool}"),
            new("server.address", "private-host"),
            new("server.port", 5432),
            new("db.query.text", "SELECT private-user-payload"),
            new("db.query.parameter.id", "private-user-id"),
        };
        if (state is not null) {
            tags.Add(new KeyValuePair<string, object?>("db.client.connection.state", state));
        }
        return [.. tags];
    }

    [ExcludeFromCodeCoverage]
    private sealed record Sample(string Name, string? Unit, double Value, long Count,
        Dictionary<string, string> Tags, double[] Bounds, bool HasExemplars);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingExporter : BaseExporter<Metric> {
        public ConcurrentQueue<Sample> Samples { get; } = new();

        public override ExportResult Export(in Batch<Metric> batch) {
            foreach (Metric metric in batch) {
                if (!string.Equals(metric.MeterName, "Npgsql", StringComparison.Ordinal)) {
                    continue;
                }
                foreach (ref readonly MetricPoint point in metric.GetMetricPoints()) {
                    var tags = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (KeyValuePair<string, object?> tag in point.Tags) {
                        tags.Add(tag.Key, tag.Value?.ToString() ?? string.Empty);
                    }
                    bool hasExemplars = point.TryGetExemplars(out ReadOnlyExemplarCollection exemplars) &&
                        exemplars.GetEnumerator().MoveNext();
                    var bounds = new List<double>();
                    bool histogram = metric.MetricType == MetricType.Histogram;
                    if (histogram) {
                        foreach (HistogramBucket bucket in point.GetHistogramBuckets()) {
                            bounds.Add(bucket.ExplicitBound);
                        }
                    }
                    Samples.Enqueue(new Sample(metric.Name, metric.Unit,
                        histogram ? point.GetHistogramSum() : point.GetSumLong(),
                        histogram ? point.GetHistogramCount() : 0, tags, [.. bounds], hasExemplars));
                }
            }
            return ExportResult.Success;
        }
    }
}
