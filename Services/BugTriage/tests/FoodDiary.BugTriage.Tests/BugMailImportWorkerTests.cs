using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Options;
using FoodDiary.BugTriage.Infrastructure.Workers;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class BugMailImportWorkerTests {
    [Fact]
    public async Task ExecuteAsync_WhenAlreadyCanceled_DoesNotStartImport() {
        IBugReportStore store = Substitute.For<IBugReportStore>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var worker = new BugMailImportWorker(new ImportBugReports(Substitute.For<IBugMailSource>(), store, TimeProvider.System),
            Microsoft.Extensions.Options.Options.Create(new BugTriageOptions()), new RecordingLogger());

        // BackgroundService short-circuits canceled startup before invoking the worker's entrypoint.
        System.Reflection.MethodInfo execute = typeof(BugMailImportWorker).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await ((Task)execute.Invoke(worker, [cancellation.Token])!).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Empty(store.ReceivedCalls());
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopAsync_CancelsActiveImportWithoutLoggingPrivateFailure(bool failFirstPoll) {
        IBugReportStore store = Substitute.For<IBugReportStore>();
        IBugMailSource source = Substitute.For<IBugMailSource>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var logger = new RecordingLogger();
        int attempts = 0;
        store.PurgeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(async call => {
            if (failFirstPoll && Interlocked.Increment(ref attempts) == 1) {
                throw new InvalidOperationException("private@example.test credential=secret");
            }
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>()).ConfigureAwait(false);
        });
        using var worker = new BugMailImportWorker(new ImportBugReports(source, store, TimeProvider.System),
            Microsoft.Extensions.Options.Options.Create(new BugTriageOptions { PollInterval = TimeSpan.Zero }), logger);

        await worker.StartAsync(CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
        Assert.Equal(failFirstPoll ? 1 : 0, logger.Messages.Count);
        if (failFirstPoll) {
            Assert.Equal("Bug mail import failed; the next polling cycle will retry.", Assert.Single(logger.Messages));
        }
        source.DidNotReceiveWithAnyArgs().ReadNewAsync(default);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger : ILogger<BugMailImportWorker> {
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
