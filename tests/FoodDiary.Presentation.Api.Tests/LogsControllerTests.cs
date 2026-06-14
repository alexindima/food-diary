using FoodDiary.Presentation.Api.Features.Logs;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class LogsControllerTests {
    [Theory]
    [InlineData("error", LogLevel.Error)]
    [InlineData("warning", LogLevel.Warning)]
    [InlineData("info", LogLevel.Information)]
    [InlineData("debug", LogLevel.Information)]
    public async Task Create_MapsClientLogLevelAndRecordsTelemetry(string level, LogLevel expectedLogLevel) {
        var logger = new RecordingLogger();
        var summaryService = new RecordingFastingTelemetrySummaryService();
        LogsController controller = CreateController(logger, summaryService);
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: level,
            Timestamp: DateTime.UtcNow.ToString("O"),
            Message: "message");

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(expectedLogLevel, logger.LogLevel);
        Assert.Same(request, summaryService.Request);
    }

    [Fact]
    public async Task Create_WithDetails_LogsRawDetails() {
        var logger = new RecordingLogger();
        var summaryService = new RecordingFastingTelemetrySummaryService();
        LogsController controller = CreateController(logger, summaryService);
        JsonElement details = JsonSerializer.Deserialize<JsonElement>("""
            {"source":"test"}
            """);
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"),
            Details: details);

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Contains("\"source\":\"test\"", logger.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WhenLoggerScopeIsNull_ReturnsNoContent() {
        var logger = new RecordingLogger(returnNullScope: true);
        var summaryService = new RecordingFastingTelemetrySummaryService();
        LogsController controller = CreateController(logger, summaryService);
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"));

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(LogLevel.Information, logger.LogLevel);
    }

    private static LogsController CreateController(RecordingLogger logger, IFastingTelemetrySummaryService summaryService) =>
        new(logger, summaryService) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger(bool returnNullScope = false) : ILogger<LogsController> {
        public LogLevel LogLevel { get; private set; }
        public string Message { get; private set; } = string.Empty;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            returnNullScope ? null : NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            LogLevel = logLevel;
            Message = formatter(state, exception);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingFastingTelemetrySummaryService : IFastingTelemetrySummaryService {
        public ClientTelemetryLogHttpRequest? Request { get; private set; }

        public Task RecordAsync(ClientTelemetryLogHttpRequest request, CancellationToken cancellationToken) {
            Request = request;
            return Task.CompletedTask;
        }

        public Task<FastingTelemetrySummarySnapshot> GetSummaryAsync(int windowHours, CancellationToken cancellationToken) {
            return Task.FromResult(new FastingTelemetrySummarySnapshot(
                windowHours,
                DateTime.UtcNow,
                StartedSessions: 0,
                CompletedSessions: 0,
                SavedCheckIns: 0,
                ReminderPresetSelections: 0,
                ReminderTimingSaves: 0,
                PresetReminderTimingSaves: 0,
                ManualReminderTimingSaves: 0,
                CompletionRatePercent: 0,
                CheckInRatePercent: 0,
                AverageCompletedDurationHours: null,
                LastCheckInAtUtc: null,
                LastEventAtUtc: null,
                TopPresets: []));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullScope : IDisposable {
        public static NullScope Instance { get; } = new();

        public void Dispose() {
        }
    }
}
