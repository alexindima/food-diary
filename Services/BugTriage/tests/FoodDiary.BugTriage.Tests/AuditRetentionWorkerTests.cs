using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Infrastructure.Options;
using FoodDiary.BugTriage.Infrastructure.Workers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuditRetentionWorkerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cleanup_ContinuesPollingAndStopsWithoutLoggingPrivateExceptions(bool failFirstPoll) {
        IAuditRunStore store = Substitute.For<IAuditRunStore>();
        DateTimeOffset now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        var secondPoll = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new RecordingLogger();
        int attempts = 0;
        store.PurgeAsync(now, Arg.Any<CancellationToken>()).Returns(async call => {
            Assert.True(call.Arg<CancellationToken>().CanBeCanceled);
            if (Interlocked.Increment(ref attempts) == 1) {
                if (failFirstPoll) { throw new InvalidOperationException("private@example.test sensitive-content"); }
                return;
            }
            secondPoll.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>()).ConfigureAwait(false);
        });
        using var worker = new AuditRetentionWorker(store, new FixedTimeProvider(now),
            Options.Create(new BugTriageOptions { PollInterval = TimeSpan.Zero }), logger);

        await worker.StartAsync(CancellationToken.None);
        try {
            await secondPoll.Task.WaitAsync(TimeSpan.FromSeconds(10));
        } finally {
            await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        }

        Assert.Multiple(
            () => Assert.Equal(2, attempts),
            () => Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully),
            () => Assert.Equal(failFirstPoll ? 1 : 0, logger.Messages.Count));
        if (failFirstPoll) {
            Assert.Equal("Audit content cleanup failed; the next polling cycle will retry.", Assert.Single(logger.Messages));
        }
        await store.Received(2).PurgeAsync(now, Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger : ILogger<AuditRetentionWorker> {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            Assert.Null(exception);
            Assert.Equal(LogLevel.Warning, logLevel);
            Messages.Add(formatter(state, exception));
        }
    }
}
