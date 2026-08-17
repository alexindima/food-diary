using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Application.Runtime.Common.Services;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public class BehaviorTests {
    [Fact]
    public async Task LoggingBehavior_WhenHandlerSucceeds_ReturnsSuccess() {
        NullLogger<LoggingBehavior<TestQuery, Result<string>>> logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);
        var query = new TestQuery();

        Result<string> result = await behavior.Handle(
            query,
            ct => Task.FromResult(Result.Success("ok")),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task LoggingBehavior_WhenHandlerFails_ReturnsFailure() {
        NullLogger<LoggingBehavior<TestQuery, Result<string>>> logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);
        var error = new Error("Test.Error", "Something went wrong");

        Result<string> result = await behavior.Handle(
            new TestQuery(),
            ct => Task.FromResult(Result.Failure<string>(error)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Test.Error", result.Error.Code);
    }

    [Theory]
    [InlineData("Authentication.InvalidCredentials", null, LogLevel.Information)]
    [InlineData("Validation.Invalid", ErrorKind.Validation, LogLevel.Information)]
    [InlineData("User.Forbidden", ErrorKind.Forbidden, LogLevel.Information)]
    [InlineData("Billing.ProviderOperationFailed", ErrorKind.ExternalFailure, LogLevel.Warning)]
    [InlineData("Test.Error", null, LogLevel.Warning)]
    public async Task LoggingBehavior_WhenHandlerFails_UsesExpectedLogLevel(
        string errorCode,
        ErrorKind? errorKind,
        LogLevel expectedLevel) {
        var logger = new RecordingLogger<LoggingBehavior<TestQuery, Result<string>>>();
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);
        var error = new Error(errorCode, "Something went wrong", Kind: errorKind);

        Result<string> result = await behavior.Handle(
            new TestQuery(),
            ct => Task.FromResult(Result.Failure<string>(error)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(expectedLevel, logger.LastLogLevel);
    }

    [Fact]
    public async Task LoggingBehavior_WhenHandlerThrows_RethrowsException() {
        NullLogger<LoggingBehavior<TestQuery, Result<string>>> logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TestQuery(),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));
    }

    [Fact]
    public async Task CommandTransactionBehavior_WhenHandlerSucceeds_SavesThenFlushesPostCommitActions() {
        var callOrder = new List<string>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => {
                callOrder.Add("save");
                return Task.CompletedTask;
            });
        IPostCommitActionQueue postCommitActionQueue = Substitute.For<IPostCommitActionQueue>();
        postCommitActionQueue.HasActions.Returns(returnThis: true);
        postCommitActionQueue
            .FlushAsync(Arg.Any<CancellationToken>())
            .Returns(_ => {
                callOrder.Add("flush");
                return Task.CompletedTask;
            });
        var behavior = new CommandTransactionBehavior<TestCommand, Result<string>>(unitOfWork, postCommitActionQueue);

        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ => {
                callOrder.Add("handler");
                return Task.FromResult(Result.Success("saved"));
            },
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(["handler", "save", "flush"], callOrder);
    }

    [Fact]
    public async Task CommandTransactionBehavior_WhenSaveChangesFails_DoesNotFlushPostCommitActions() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("commit failed"));
        IPostCommitActionQueue postCommitActionQueue = Substitute.For<IPostCommitActionQueue>();
        postCommitActionQueue.HasActions.Returns(returnThis: true);
        var behavior = new CommandTransactionBehavior<TestCommand, Result<string>>(unitOfWork, postCommitActionQueue);

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success("saved")),
            CancellationToken.None));

        await postCommitActionQueue.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CommandTransactionBehavior_WhenRequestIsCanceledAfterSave_FlushesPostCommitActionsWithIndependentToken() {
        using var cancellationTokenSource = new CancellationTokenSource();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(async _ => await cancellationTokenSource.CancelAsync().ConfigureAwait(false));
        IPostCommitActionQueue postCommitActionQueue = Substitute.For<IPostCommitActionQueue>();
        postCommitActionQueue.HasActions.Returns(returnThis: true);
        postCommitActionQueue
            .FlushAsync(Arg.Any<CancellationToken>())
            .Returns(call => {
                Assert.False(call.Arg<CancellationToken>().IsCancellationRequested);
                return Task.CompletedTask;
            });
        var behavior = new CommandTransactionBehavior<TestCommand, Result<string>>(unitOfWork, postCommitActionQueue);

        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Success("saved")),
            cancellationTokenSource.Token);

        ResultAssert.Success(result);
        await postCommitActionQueue.Received(1).FlushAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CommandTransactionBehavior_WhenHandlerReturnsFailure_DoesNotSaveOrFlushPostCommitActions() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        IPostCommitActionQueue postCommitActionQueue = Substitute.For<IPostCommitActionQueue>();
        postCommitActionQueue.HasActions.Returns(returnThis: true);
        var error = new Error("Test.Failed", "The command failed.");
        var behavior = new CommandTransactionBehavior<TestCommand, Result<string>>(unitOfWork, postCommitActionQueue);

        Result<string> result = await behavior.Handle(
            new TestCommand(),
            _ => Task.FromResult(Result.Failure<string>(error)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await postCommitActionQueue.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostCommitActionQueue_FlushAsync_DrainsActionsEnqueuedDuringFlush() {
        var postCommitActionQueue = new PostCommitActionQueue(
            NullLogger<PostCommitActionQueue>.Instance,
            TimeProvider.System);
        var callOrder = new List<string>();
        postCommitActionQueue.Enqueue("test.first", _ => {
            callOrder.Add("first");
            postCommitActionQueue.Enqueue("test.second", _ => {
                callOrder.Add("second");
                return Task.CompletedTask;
            });

            return Task.CompletedTask;
        });

        await postCommitActionQueue.FlushAsync(CancellationToken.None);

        Assert.Equal(["first", "second"], callOrder);
        Assert.False(postCommitActionQueue.HasActions);
    }

    [Fact]
    public async Task PostCommitActionQueue_FlushAsync_WhenActionFails_LogsWarningAndContinues() {
        var logger = new RecordingLogger<PostCommitActionQueue>();
        var postCommitActionQueue = new PostCommitActionQueue(logger, TimeProvider.System);
        var callOrder = new List<string>();
        postCommitActionQueue.Enqueue("test.failing", _ => throw new InvalidOperationException("failed"));
        postCommitActionQueue.Enqueue("test.next", _ => {
            callOrder.Add("next");
            return Task.CompletedTask;
        });

        await postCommitActionQueue.FlushAsync(CancellationToken.None);

        Assert.Equal(LogLevel.Warning, logger.LastLogLevel);
        Assert.Equal(["next"], callOrder);
        Assert.False(postCommitActionQueue.HasActions);
    }

    [Fact]
    public async Task PostCommitActionQueue_FlushAsync_WhenCancellationRequested_PropagatesCancellation() {
        var postCommitActionQueue = new PostCommitActionQueue(
            NullLogger<PostCommitActionQueue>.Instance,
            TimeProvider.System);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        postCommitActionQueue.Enqueue("test.cancel", cancellationToken => Task.FromCanceled(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => postCommitActionQueue.FlushAsync(cts.Token));
    }

    [Fact]
    public async Task PostCommitActionQueue_FlushAsync_WhenActionTimesOut_ContinuesWithNextAction() {
        var postCommitActionQueue = new PostCommitActionQueue(
            NullLogger<PostCommitActionQueue>.Instance,
            TimeProvider.System,
            TimeSpan.FromMilliseconds(10));
        bool nextActionExecuted = false;
        postCommitActionQueue.Enqueue(
            "test.stuck",
            static cancellationToken => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        postCommitActionQueue.Enqueue(
            "test.next",
            _ => {
                nextActionExecuted = true;
                return Task.CompletedTask;
            });

        await postCommitActionQueue.FlushAsync();

        Assert.True(nextActionExecuted);
    }

    [Fact]
    public async Task PostCommitActionQueue_Enqueue_WhenCapacityReached_DropsOverflowWithoutFailing() {
        var postCommitActionQueue = new PostCommitActionQueue(
            NullLogger<PostCommitActionQueue>.Instance,
            TimeProvider.System,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            maxActions: 2);
        var executedActions = new List<string>();
        postCommitActionQueue.Enqueue("test.first", _ => {
            executedActions.Add("first");
            return Task.CompletedTask;
        });
        postCommitActionQueue.Enqueue("test.second", _ => {
            executedActions.Add("second");
            return Task.CompletedTask;
        });

        postCommitActionQueue.Enqueue("test.overflow", _ => {
            executedActions.Add("overflow");
            return Task.CompletedTask;
        });
        await postCommitActionQueue.FlushAsync();
        postCommitActionQueue.Enqueue("test.next-cycle", _ => {
            executedActions.Add("next-cycle");
            return Task.CompletedTask;
        });
        await postCommitActionQueue.FlushAsync();

        Assert.Equal(["first", "second", "next-cycle"], executedActions);
        Assert.False(postCommitActionQueue.HasActions);
    }

    [Fact]
    public async Task PostCommitActionQueue_FlushAsync_WhenTotalBudgetExpires_DropsUnstartedActions() {
        var postCommitActionQueue = new PostCommitActionQueue(
            NullLogger<PostCommitActionQueue>.Instance,
            TimeProvider.System,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(20),
            maxActions: 4);
        bool secondActionExecuted = false;
        postCommitActionQueue.Enqueue(
            "test.total-timeout",
            static cancellationToken => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken));
        postCommitActionQueue.Enqueue(
            "test.unstarted",
            _ => {
                secondActionExecuted = true;
                return Task.CompletedTask;
            });

        await postCommitActionQueue.FlushAsync();

        Assert.False(secondActionExecuted);
        Assert.False(postCommitActionQueue.HasActions);
    }

    [Fact]
    public async Task PostCommitActionQueue_RecordsBoundedOutcomesDepthAndDuration() {
        var measurements = new ConcurrentBag<MetricMeasurement>();
        using var listener = new MeterListener {
            InstrumentPublished = (instrument, meterListener) => {
                if (string.Equals(instrument.Meter.Name, ApplicationRuntimeTelemetry.MeterName, StringComparison.Ordinal)) {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            measurements.Add(CreateMeasurement(instrument.Name, measurement, tags)));
        listener.SetMeasurementEventCallback<int>((instrument, measurement, tags, _) =>
            measurements.Add(CreateMeasurement(instrument.Name, measurement, tags)));
        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            measurements.Add(CreateMeasurement(instrument.Name, measurement, tags)));
        listener.Start();
        var postCommitActionQueue = new PostCommitActionQueue(
            NullLogger<PostCommitActionQueue>.Instance,
            TimeProvider.System,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            maxActions: 1);
        postCommitActionQueue.Enqueue("test.success", _ => Task.CompletedTask);
        postCommitActionQueue.Enqueue("test.overflow", _ => Task.CompletedTask);

        await postCommitActionQueue.FlushAsync();

        Assert.Contains(measurements, measurement =>
            string.Equals(
                measurement.Name,
                "fooddiary.application.post_commit.queue_depth",
                StringComparison.Ordinal) && measurement.Value == 1);
        Assert.Contains(measurements, measurement =>
            string.Equals(
                measurement.Name,
                "fooddiary.application.post_commit.actions",
                StringComparison.Ordinal) &&
            string.Equals(measurement.Outcome, "succeeded", StringComparison.Ordinal));
        Assert.Contains(measurements, measurement =>
            string.Equals(
                measurement.Name,
                "fooddiary.application.post_commit.actions",
                StringComparison.Ordinal) &&
            string.Equals(measurement.Outcome, "dropped", StringComparison.Ordinal) &&
            string.Equals(measurement.Reason, "capacity", StringComparison.Ordinal));
        Assert.Contains(measurements, measurement =>
            string.Equals(
                measurement.Name,
                "fooddiary.application.post_commit.flush_duration",
                StringComparison.Ordinal));
        Assert.All(
            measurements.Where(static measurement => measurement.Outcome is not null),
            measurement => Assert.True(measurement.Outcome is "succeeded" or "failed" or "timed_out" or "dropped"));
    }

    private static MetricMeasurement CreateMeasurement<T>(
        string name,
        T value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
        where T : struct {
        string? outcome = null;
        string? reason = null;
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, "fooddiary.post_commit.outcome", StringComparison.Ordinal)) {
                outcome = tag.Value as string;
            } else if (string.Equals(tag.Key, "fooddiary.post_commit.reason", StringComparison.Ordinal)) {
                reason = tag.Value as string;
            }
        }

        return new MetricMeasurement(name, Convert.ToDouble(value, CultureInfo.InvariantCulture), outcome, reason);
    }

    [ExcludeFromCodeCoverage]
    private record TestQuery : IQuery<Result<string>>;

    [ExcludeFromCodeCoverage]
    private record TestCommand : ICommand<Result<string>>;

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public LogLevel LastLogLevel { get; private set; } = LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            LastLogLevel = logLevel;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record MetricMeasurement(string Name, double Value, string? Outcome, string? Reason);

    [ExcludeFromCodeCoverage]
    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
