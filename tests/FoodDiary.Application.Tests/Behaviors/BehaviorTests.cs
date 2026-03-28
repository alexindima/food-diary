using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Behaviors;

public class BehaviorTests {
    [Fact]
    public async Task LoggingBehavior_WhenHandlerSucceeds_ReturnsSuccess() {
        var logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);
        var query = new TestQuery();

        var result = await behavior.Handle(
            query,
            ct => Task.FromResult(Result.Success("ok")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task LoggingBehavior_WhenHandlerFails_ReturnsFailure() {
        var logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);
        var error = new Error("Test.Error", "Something went wrong");

        var result = await behavior.Handle(
            new TestQuery(),
            ct => Task.FromResult(Result.Failure<string>(error)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Test.Error", result.Error.Code);
    }

    [Fact]
    public async Task LoggingBehavior_WhenHandlerThrows_RethrowsException() {
        var logger = NullLogger<LoggingBehavior<TestQuery, Result<string>>>.Instance;
        var behavior = new LoggingBehavior<TestQuery, Result<string>>(logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TestQuery(),
                _ => { throw new InvalidOperationException("boom"); },
                CancellationToken.None));
    }

    [Fact]
    public async Task UserIdValidationBehavior_WhenUserIdNull_ReturnsFailure() {
        var behavior = new UserIdValidationBehavior<TestUserCommand, Result<string>>();
        var command = new TestUserCommand(null);

        var result = await behavior.Handle(
            command,
            ct => throw new InvalidOperationException("Should not reach handler"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UserIdValidationBehavior_WhenUserIdEmpty_ReturnsFailure() {
        var behavior = new UserIdValidationBehavior<TestUserCommand, Result<string>>();
        var command = new TestUserCommand(Guid.Empty);

        var result = await behavior.Handle(
            command,
            ct => throw new InvalidOperationException("Should not reach handler"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UserIdValidationBehavior_WhenUserIdValid_CallsNext() {
        var behavior = new UserIdValidationBehavior<TestUserCommand, Result<string>>();
        var command = new TestUserCommand(Guid.NewGuid());

        var result = await behavior.Handle(
            command,
            ct => Task.FromResult(Result.Success("passed")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("passed", result.Value);
    }

    [Fact]
    public async Task UserIdValidationBehavior_WhenNonGenericResult_ReturnsFailure() {
        var behavior = new UserIdValidationBehavior<TestUserVoidCommand, Result>();
        var command = new TestUserVoidCommand(null);

        var result = await behavior.Handle(
            command,
            ct => throw new InvalidOperationException("Should not reach handler"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private record TestQuery : IQuery<Result<string>>;

    private record TestUserCommand(Guid? UserId) : ICommand<Result<string>>, IUserRequest;

    private record TestUserVoidCommand(Guid? UserId) : ICommand<Result>, IUserRequest;
}
