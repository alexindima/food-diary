using FoodDiary.Telegram.Bot.Operations;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
public sealed class TelegramLoopTelemetryTests {
    [Theory]
    [InlineData(true, "intake")]
    [InlineData(false, "discovery")]
    public void ReportsFirstPollThenAggregatesAndResetsEveryMinute(bool intake, string name) {
        var clock = new Clock();
        var logger = new RecordingLogger();
        var telemetry = new TelegramLoopTelemetry(intake ? TelegramLoopKind.Intake : TelegramLoopKind.Discovery, clock, logger);
        telemetry.Observe(success: true, TimeSpan.FromMilliseconds(10));
        Dictionary<string, object?> first = Assert.Single(logger.Events);
        Assert.Equal(name, first["Loop"]);
        Assert.Equal(1L, first["SuccessfulPolls"]);
        Assert.Equal(0L, first["FailedPolls"]);
        clock.Advance(TimeSpan.FromSeconds(30));
        telemetry.Observe(success: true, TimeSpan.FromMilliseconds(20));
        telemetry.Observe(success: false, TimeSpan.FromMilliseconds(40));
        Assert.Single(logger.Events);
        clock.Advance(TimeSpan.FromSeconds(30));
        telemetry.Observe(success: true, TimeSpan.FromMilliseconds(60));
        Dictionary<string, object?> second = logger.Events[1];
        Assert.Multiple(
            () => Assert.Equal(2L, second["SuccessfulPolls"]),
            () => Assert.Equal(1L, second["FailedPolls"]),
            () => Assert.Equal(40L, second["AverageDurationMs"]),
            () => Assert.Equal(clock.GetUtcNow().ToUnixTimeSeconds(), second["LastSuccessUnixSeconds"]));
        clock.Advance(TimeSpan.FromMinutes(1));
        telemetry.Observe(success: false, TimeSpan.FromMilliseconds(5));
        Dictionary<string, object?> third = logger.Events[2];
        Assert.Equal(0L, third["SuccessfulPolls"]);
        Assert.Equal(1L, third["FailedPolls"]);
        Assert.Equal(second["LastSuccessUnixSeconds"], third["LastSuccessUnixSeconds"]);
    }

    [Fact]
    public void WallClockJumpDoesNotFloodSummariesOrInventSuccess() {
        var clock = new Clock();
        var logger = new RecordingLogger();
        var telemetry = new TelegramLoopTelemetry(TelegramLoopKind.Intake, clock, logger);
        telemetry.Observe(success: false, TimeSpan.FromSeconds(1));
        clock.UtcNow = clock.UtcNow.AddDays(1);
        for (int index = 0; index < 100; index++) {
            telemetry.Observe(success: false, TimeSpan.FromSeconds(1));
        }
        Dictionary<string, object?> entry = Assert.Single(logger.Events);
        Assert.Equal(0L, entry["LastSuccessUnixSeconds"]);
        string[] expected = ["AverageDurationMs", "FailedPolls", "LastSuccessUnixSeconds", "Loop", "SuccessfulPolls", "{OriginalFormat}"];
        Assert.Equal(expected, entry.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Clock : TimeProvider {
        private long _timestamp;
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-11-08T00:00:00Z", CultureInfo.InvariantCulture);
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _timestamp;
        public override DateTimeOffset GetUtcNow() => UtcNow;
        public void Advance(TimeSpan duration) {
            _timestamp += duration.Ticks;
            UtcNow += duration;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger : ILogger {
        public List<Dictionary<string, object?>> Events { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            Assert.Equal(LogLevel.Information, logLevel);
            Assert.Null(exception);
            IEnumerable<KeyValuePair<string, object?>> fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(state);
            Events.Add(fields.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal));
        }
    }
}
