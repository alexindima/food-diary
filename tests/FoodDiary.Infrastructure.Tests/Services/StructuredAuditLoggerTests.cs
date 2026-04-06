using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class StructuredAuditLoggerTests {
    private static readonly DateTime FixedUtcNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Log_WritesInformationLogEntry() {
        var logger = new RecordingLogger();
        var auditLogger = new StructuredAuditLogger(logger, new StubDateTimeProvider());
        var actorId = UserId.New();

        auditLogger.Log("user.created", actorId, "User", actorId.Value.ToString(), "Created via signup");

        Assert.Equal(1, logger.LogCount);
        Assert.Equal(LogLevel.Information, logger.LastLogLevel);
    }

    [Fact]
    public void Log_IncludesActionAndActorInMessage() {
        var logger = new RecordingLogger();
        var auditLogger = new StructuredAuditLogger(logger, new StubDateTimeProvider());
        var actorId = UserId.New();

        auditLogger.Log("user.deleted", actorId, "User", "123", null);

        Assert.Contains("user.deleted", logger.LastMessage);
        Assert.Contains(actorId.Value.ToString(), logger.LastMessage);
    }

    [Fact]
    public void Log_WithNullOptionalFields_UsesDashPlaceholder() {
        var logger = new RecordingLogger();
        var auditLogger = new StructuredAuditLogger(logger, new StubDateTimeProvider());
        var actorId = UserId.New();

        auditLogger.Log("test.action", actorId, null, null, null);

        Assert.Contains("-", logger.LastMessage);
    }

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => FixedUtcNow;
    }

    private sealed class RecordingLogger : ILogger<StructuredAuditLogger> {
        public int LogCount { get; private set; }
        public LogLevel LastLogLevel { get; private set; }
        public string LastMessage { get; private set; } = string.Empty;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) {
            LogCount++;
            LastLogLevel = logLevel;
            LastMessage = formatter(state, exception);
        }

        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
