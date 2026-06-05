using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Behaviors;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Behaviors;

[ExcludeFromCodeCoverage]
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
    public async Task UnitOfWorkBehavior_WhenPendingChanges_SavesAfterHandler() {
        var unitOfWork = new FakeUnitOfWork(hasPendingChanges: true);
        var behavior = new UnitOfWorkBehavior<TestCommand, Result<string>>(unitOfWork);

        var result = await behavior.Handle(
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
}
