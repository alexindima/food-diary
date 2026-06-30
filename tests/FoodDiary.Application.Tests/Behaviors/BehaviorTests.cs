using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Behaviors;
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
    public async Task UnitOfWorkBehavior_WhenPendingChanges_SavesAfterHandler() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var behavior = new UnitOfWorkBehavior<TestCommand, Result<string>>(unitOfWork);

        Result<string> result = await behavior.Handle(
            new TestCommand(),
            ct => Task.FromResult(Result.Success("saved")),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("saved", result.Value);
        await unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PostCommitBehavior_WhenPostCommitActionsExist_FlushesAfterHandler() {
        IPostCommitActionQueue postCommitActionQueue = Substitute.For<IPostCommitActionQueue>();
        postCommitActionQueue.HasActions.Returns(returnThis: true);
        postCommitActionQueue.FlushAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var behavior = new PostCommitBehavior<TestCommand, Result<string>>(postCommitActionQueue);
        bool handlerCalled = false;

        Result<string> result = await behavior.Handle(
            new TestCommand(),
            ct => {
                handlerCalled = true;
                return Task.FromResult(Result.Success("saved"));
            },
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(handlerCalled);
        await postCommitActionQueue.Received(requiredNumberOfCalls: 1).FlushAsync(CancellationToken.None);
    }

    [Fact]
    public async Task PostCommitAndUnitOfWorkBehaviors_WhenComposed_FlushAfterSuccessfulSaveChanges() {
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
        var postCommitBehavior = new PostCommitBehavior<TestCommand, Result<string>>(postCommitActionQueue);
        var unitOfWorkBehavior = new UnitOfWorkBehavior<TestCommand, Result<string>>(unitOfWork);

        Result<string> result = await postCommitBehavior.Handle(
            new TestCommand(),
            ct => unitOfWorkBehavior.Handle(
                new TestCommand(),
                _ => {
                    callOrder.Add("handler");
                    return Task.FromResult(Result.Success("saved"));
                },
                ct),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(["handler", "save", "flush"], callOrder);
    }

    [Fact]
    public async Task PostCommitAndUnitOfWorkBehaviors_WhenSaveChangesFails_DoNotFlushPostCommitActions() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("commit failed"));
        IPostCommitActionQueue postCommitActionQueue = Substitute.For<IPostCommitActionQueue>();
        var postCommitBehavior = new PostCommitBehavior<TestCommand, Result<string>>(postCommitActionQueue);
        var unitOfWorkBehavior = new UnitOfWorkBehavior<TestCommand, Result<string>>(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => postCommitBehavior.Handle(
            new TestCommand(),
            ct => unitOfWorkBehavior.Handle(
                new TestCommand(),
                _ => Task.FromResult(Result.Success("saved")),
                ct),
            CancellationToken.None));

        await postCommitActionQueue.DidNotReceive().FlushAsync(Arg.Any<CancellationToken>());
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
