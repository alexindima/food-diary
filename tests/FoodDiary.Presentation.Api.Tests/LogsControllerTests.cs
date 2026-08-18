using FoodDiary.Results;
using FoodDiary.Application.Fasting.Commands.RecordFastingTelemetry;
using FoodDiary.Presentation.Api.Features.Logs;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class LogsControllerTests {
    [Theory]
    [InlineData("error", LogLevel.Warning)]
    [InlineData("warning", LogLevel.Warning)]
    [InlineData("info", LogLevel.Information)]
    public async Task Create_MapsClientLogLevelAndRecordsTelemetry(string level, LogLevel expectedLogLevel) {
        var logger = new RecordingLogger();
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        LogsController controller = CreateController(logger, sender);
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: level,
            Timestamp: DateTime.UtcNow.ToString("O"),
            Message: "message");

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(expectedLogLevel, logger.LogLevel);
        RecordFastingTelemetryCommand command = Assert.IsType<RecordFastingTelemetryCommand>(sentRequest);
        Assert.Equal(request.Category, command.Category);
        Assert.Equal(request.Name, command.Name);
        Assert.Equal(request.Timestamp, command.Timestamp);
    }

    [Fact]
    public async Task Create_WithNonFastingTelemetry_LogsWithoutDispatchingMediatorCommand() {
        var logger = new RecordingLogger();
        int sentRequests = 0;
        ISender sender = SubstituteSender.Create(Result.Success(), _ => sentRequests++);
        LogsController controller = CreateController(logger, sender);
        var request = new ClientTelemetryLogHttpRequest(
            Category: "http_request",
            Name: "api.request",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"));

        IActionResult result = await controller.Create(request);

        Assert.Multiple(
            () => Assert.IsType<NoContentResult>(result),
            () => Assert.Equal(0, sentRequests),
            () => Assert.Equal(LogLevel.Information, logger.LogLevel));
    }

    [Fact]
    public async Task Create_WithSensitivePayload_DoesNotLogRawValues() {
        var logger = new RecordingLogger();
        LogsController controller = CreateController(logger, SubstituteSender.Create(Result.Success()));
        JsonElement details = JsonSerializer.Deserialize<JsonElement>("""
            {"source":"test"}
            """);
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"),
            Message: "secret-message",
            Location: "https://example.test/?token=secret-location",
            Route: "/route?token=secret-route",
            SessionId: "secret-session",
            Stack: "secret-stack",
            Details: details);

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Multiple(
            () => Assert.DoesNotContain("secret-message", logger.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain("secret-location", logger.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain("secret-route", logger.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain("secret-session", logger.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain("secret-stack", logger.Message, StringComparison.Ordinal),
            () => Assert.DoesNotContain("\"source\":\"test\"", logger.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Request_WithUnknownEventName_IsInvalid() {
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.attacker-controlled",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"));
        List<ValidationResult> validationResults = [];

        bool valid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(request.Name), StringComparer.Ordinal));
    }

    [Fact]
    public void Request_WithOversizedDetails_IsInvalid() {
        JsonElement details = JsonSerializer.SerializeToElement(new { value = new string('x', 5000) });
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"),
            Details: details);
        List<ValidationResult> validationResults = [];

        bool valid = Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(request.Details), StringComparer.Ordinal));
    }

    [Fact]
    public async Task Create_WhenLoggerScopeIsNull_ReturnsNoContent() {
        var logger = new RecordingLogger(returnNullScope: true);
        LogsController controller = CreateController(logger, SubstituteSender.Create(Result.Success()));
        var request = new ClientTelemetryLogHttpRequest(
            Category: "user_action",
            Name: "fasting.session.started",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"));

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(LogLevel.Information, logger.LogLevel);
    }

    [Fact]
    public async Task Create_NormalizesBoundedLogDimensions() {
        var logger = new RecordingLogger();
        LogsController controller = CreateController(logger, SubstituteSender.Create(Result.Success()));
        var request = new ClientTelemetryLogHttpRequest(
            Category: "http_request",
            Name: "api.request",
            Level: "info",
            Timestamp: DateTime.UtcNow.ToString("O"),
            PageRoute: "/" + new string('p', 300),
            HttpMethod: "GET\n",
            Outcome: " success\t",
            Unit: " ms\r",
            BuildVersion: " build\n");

        IActionResult result = await controller.Create(request);

        Assert.IsType<NoContentResult>(result);
        Assert.Multiple(
            () => Assert.DoesNotContain('\n', logger.Message),
            () => Assert.DoesNotContain('\r', logger.Message),
            () => Assert.DoesNotContain('\t', logger.Message),
            () => Assert.DoesNotContain(new string('p', 257), logger.Message, StringComparison.Ordinal),
            () => Assert.Contains("success", logger.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Processor_WithCanceledRequest_DoesNotAwaitApplicationResult() {
        var logger = new RecordingLogger();
        var processor = new ClientTelemetryHttpProcessor(SubstituteSender.Create(Result.Success()), logger);
        ClientTelemetryLogHttpRequest request = new(
            "http_request", "api.request", "info", DateTime.UtcNow.ToString("O"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            processor.ProcessAsync(request, cts.Token));
    }

    private static LogsController CreateController(RecordingLogger logger, ISender sender) =>
        new(sender, new ClientTelemetryHttpProcessor(sender, logger)) {
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
    private sealed class NullScope : IDisposable {
        public static NullScope Instance { get; } = new();

        public void Dispose() {
        }
    }
}
