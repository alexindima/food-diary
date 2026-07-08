using FluentValidation;
using FoodDiary.MailRelay.Application.Common.Behaviors;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayResultAndValidationBehaviorTests {
    [Fact]
    public async Task ValidationBehavior_WhenNoValidators_InvokesNext() {
        var behavior = new MailRelayValidationBehavior<TestRequest, Result>([]);

        Result result = await behavior.Handle(
            new TestRequest(""),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidationBehavior_WhenNonGenericResultFails_ReturnsFailure() {
        var behavior = new MailRelayValidationBehavior<TestRequest, Result>([new TestRequestValidator()]);

        Result result = await behavior.Handle(
            new TestRequest(""),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
    }

    [Fact]
    public async Task ValidationBehavior_WhenResponseTypeIsUnsupported_Throws() {
        var behavior = new MailRelayValidationBehavior<UnsupportedRequest, UnsupportedResult>([new UnsupportedRequestValidator()]);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new UnsupportedRequest(""),
            _ => Task.FromResult(new UnsupportedResult()),
            CancellationToken.None));

        Assert.Contains("Unable to create failure result", ex.Message, StringComparison.Ordinal);
    }

    private sealed record TestRequest(string Name) : IRequest<Result>;

    private sealed record UnsupportedRequest(string Name) : IRequest<UnsupportedResult>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest> {
        public TestRequestValidator() {
            RuleFor(static request => request.Name).NotEmpty();
        }
    }

    private sealed class UnsupportedRequestValidator : AbstractValidator<UnsupportedRequest> {
        public UnsupportedRequestValidator() {
            RuleFor(static request => request.Name).NotEmpty();
        }
    }

    private sealed class UnsupportedResult : Result {
        public UnsupportedResult()
            : base(isSuccess: false, new Error("unsupported", "unsupported")) {
        }
    }
}
