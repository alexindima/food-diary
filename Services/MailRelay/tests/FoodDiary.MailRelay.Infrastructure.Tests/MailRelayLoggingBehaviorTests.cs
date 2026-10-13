using FoodDiary.MailRelay.Application.Common.Behaviors;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Queries.GetOutgoingEmailJournal;
using FoodDiary.Results;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailRelay.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayLoggingBehaviorTests {
    [Fact]
    public async Task Handle_WhenNextSucceeds_ReturnsResult() {
        MailRelayLoggingBehavior<TestRequest, Result> behavior = CreateBehavior();

        Result result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_WhenNextReturnsFailure_ReturnsFailureResult() {
        MailRelayLoggingBehavior<TestRequest, Result> behavior = CreateBehavior();
        Error error = new("test", "failed", ErrorKind.Validation);

        Result result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromResult(Result.Failure(error)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenNextThrows_LogsAndRethrows(bool requestIsCancelled) {
        var logger = new RecordingLogger<MailRelayLoggingBehavior<TestRequest, Result>>();
        var behavior = new MailRelayLoggingBehavior<TestRequest, Result>(logger);
        using var requestCancellation = new CancellationTokenSource();
        if (requestIsCancelled) {
            await requestCancellation.CancelAsync();
        }
        var expectedException = new InvalidOperationException("boom");

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TestRequest(),
            _ => throw expectedException,
            requestCancellation.Token));

        Assert.Same(expectedException, ex);
        Assert.Equal(LogLevel.Error, logger.LastLogLevel);
    }

    [Fact]
    public async Task Handle_WhenJournalValidationIsCancelled_LogsDebugAndRethrows() {
        var logger = new RecordingLogger<MailRelayLoggingBehavior<GetOutgoingEmailJournalQuery, Result<OutgoingEmailJournalPage>>>();
        var behavior = new MailRelayLoggingBehavior<GetOutgoingEmailJournalQuery, Result<OutgoingEmailJournalPage>>(logger);
        var validation = new MailRelayValidationBehavior<GetOutgoingEmailJournalQuery, Result<OutgoingEmailJournalPage>>(
            [new GetOutgoingEmailJournalQueryValidator()]);
        var request = new GetOutgoingEmailJournalQuery(Page: 1, Limit: 25, Purpose: null, Status: null, Recipient: null);
        using var requestCancellation = new CancellationTokenSource();
        await requestCancellation.CancelAsync();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(
            request,
            cancellationToken => validation.Handle(
                request,
                _ => throw new InvalidOperationException("Cancelled validation must not invoke the handler."),
                cancellationToken),
            requestCancellation.Token));

        Assert.Equal(requestCancellation.Token, exception.CancellationToken);
        Assert.Equal(LogLevel.Debug, logger.LastLogLevel);
    }

    [Theory]
    [InlineData(false, LogLevel.Error)]
    [InlineData(true, LogLevel.Debug)]
    public async Task Handle_WhenMatchingCancellationIsThrown_PreservesExceptionAndChecksRequestedState(
        bool requestIsCancelled,
        LogLevel expectedLogLevel) {
        var logger = new RecordingLogger<MailRelayLoggingBehavior<TestRequest, Result>>();
        var behavior = new MailRelayLoggingBehavior<TestRequest, Result>(logger);
        using var requestCancellation = new CancellationTokenSource();
        if (requestIsCancelled) {
            await requestCancellation.CancelAsync();
        }
        var expectedException = new OperationCanceledException(requestCancellation.Token);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(() => behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<Result>(expectedException),
            requestCancellation.Token));

        Assert.Same(expectedException, exception);
        Assert.Equal(expectedLogLevel, logger.LastLogLevel);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenUnrelatedCancellationIsThrown_LogsErrorAndRethrows(bool requestIsCancelled) {
        var logger = new RecordingLogger<MailRelayLoggingBehavior<TestRequest, Result>>();
        var behavior = new MailRelayLoggingBehavior<TestRequest, Result>(logger);
        using var requestCancellation = new CancellationTokenSource();
        using var unrelatedCancellation = new CancellationTokenSource();
        await unrelatedCancellation.CancelAsync();
        if (requestIsCancelled) {
            await requestCancellation.CancelAsync();
        }
        var expectedException = new OperationCanceledException(unrelatedCancellation.Token);

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(() => behavior.Handle(
            new TestRequest(),
            _ => Task.FromException<Result>(expectedException),
            requestCancellation.Token));

        Assert.Same(expectedException, exception);
        Assert.Equal(LogLevel.Error, logger.LastLogLevel);
    }

    private static MailRelayLoggingBehavior<TestRequest, Result> CreateBehavior() =>
        new(NullLogger<MailRelayLoggingBehavior<TestRequest, Result>>.Instance);

    [ExcludeFromCodeCoverage]
    private sealed record TestRequest : IRequest<Result>;

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public LogLevel LastLogLevel { get; private set; } = LogLevel.None;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

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
}
