using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Behaviors;
using FoodDiary.Application.Common.Services;
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
                _ => { throw new InvalidOperationException("boom"); },
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
            .Returns(async _ => {
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            });
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
        var postCommitActionQueue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance);
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
    private sealed class NullScope : IDisposable {
        public static readonly NullScope Instance = new();

        public void Dispose() {
        }
    }
}
