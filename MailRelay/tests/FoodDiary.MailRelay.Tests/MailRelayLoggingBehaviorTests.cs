using FoodDiary.MailRelay.Application.Common.Behaviors;
using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailRelay.Tests;

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
        MailRelayError error = new("test", "failed", ErrorKind.Validation);

        Result result = await behavior.Handle(
            new TestRequest(),
            _ => Task.FromResult(Result.Failure(error)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task Handle_WhenNextThrows_LogsAndRethrows() {
        MailRelayLoggingBehavior<TestRequest, Result> behavior = CreateBehavior();

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new TestRequest(),
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None));

        Assert.Equal("boom", ex.Message);
    }

    private static MailRelayLoggingBehavior<TestRequest, Result> CreateBehavior() =>
        new(NullLogger<MailRelayLoggingBehavior<TestRequest, Result>>.Instance);

    private sealed record TestRequest : IRequest<Result>;
}
