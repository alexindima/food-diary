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

        Assert.True(result.IsSuccess);
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

        Assert.True(result.IsFailure);
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

        Assert.True(result.IsFailure);
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
        var unitOfWork = new FakeUnitOfWork(hasPendingChanges: true);
        var behavior = new UnitOfWorkBehavior<TestCommand, Result<string>>(unitOfWork);

        Result<string> result = await behavior.Handle(
            new TestCommand(),
            ct => Task.FromResult(Result.Success("saved")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("saved", result.Value);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [ExcludeFromCodeCoverage]
    private record TestQuery : IQuery<Result<string>>;

    [ExcludeFromCodeCoverage]
    private record TestCommand : ICommand<Result<string>>;

    [ExcludeFromCodeCoverage]
    private sealed class FakeUnitOfWork(bool hasPendingChanges) : IUnitOfWork {
        public int SaveCount { get; private set; }
        public bool HasPendingChanges => hasPendingChanges;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

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
