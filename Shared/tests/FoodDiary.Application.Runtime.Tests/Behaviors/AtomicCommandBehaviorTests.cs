using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Application.Runtime.Common.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Runtime.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public sealed class AtomicCommandBehaviorTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdempotentCommand_RequiresTheAuthenticatedOwnerAndForwardsItsOperation(bool wrongOwner) {
        var user = Guid.NewGuid();
        var operation = new AtomicOperation(wrongOwner ? Guid.NewGuid() : user, "operation-key", "request-hash", TimeSpan.FromDays(1));
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IPostCommitActionQueue queue = Substitute.For<IPostCommitActionQueue>();
        IAtomicCommandExecutor executor = Substitute.For<IAtomicCommandExecutor>();
        executor.ExecuteAsync(operation, Arg.Any<Func<CancellationToken, Task<Result<string>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Result<string>>>>()(call.Arg<CancellationToken>()));
        var behavior = new CommandTransactionBehavior<IdempotentCommand, Result<string>>(unitOfWork, queue, executor);
        bool invoked = false;
        Task<Result<string>> pending = behavior.Handle(new IdempotentCommand(user, operation), _ => {
            invoked = true;
            return Task.FromResult(Result.Success("committed"));
        }, CancellationToken.None);

        if (wrongOwner) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => pending);
            Assert.False(invoked);
            Assert.Empty(executor.ReceivedCalls());
            unitOfWork.Received(1).DiscardChanges();
            queue.Received(1).Discard();
        } else {
            Assert.Equal("committed", ResultAssert.Success(await pending));
            Assert.True(invoked);
            await executor.Received(1).ExecuteAsync(operation, Arg.Any<Func<CancellationToken, Task<Result<string>>>>(), CancellationToken.None);
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task NestedAtomicCommand_FailsBeforeExecutingAndMarksTheOuterScopeFailed() {
        var scope = new CommandExecutionScope();
        using CommandExecutionScope.Lease outer = scope.Enter();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IAtomicCommandExecutor executor = Substitute.For<IAtomicCommandExecutor>();
        var behavior = new CommandTransactionBehavior<AtomicCommand, Result<string>>(
            unitOfWork, Substitute.For<IPostCommitActionQueue>(), executor, scope);
        bool invoked = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new AtomicCommand(), _ => {
            invoked = true;
            return Task.FromResult(Result.Success("must not commit"));
        }, CancellationToken.None));

        Assert.Multiple(() => Assert.False(invoked), () => Assert.True(scope.HasFailedNestedCommand), () => Assert.Empty(executor.ReceivedCalls()));
        unitOfWork.DidNotReceive().DiscardChanges();
    }

    [Fact]
    public void ScopeLeases_CanBeDisposedTwiceWithoutLosingTheOuterBoundary() {
        var scope = new CommandExecutionScope();
        using CommandExecutionScope.Lease outer = scope.Enter();
        using CommandExecutionScope.Lease inner = scope.Enter();
        inner.Dispose();
        inner.Dispose();
        using CommandExecutionScope.Lease nextInner = scope.Enter();
        Assert.False(nextInner.IsOutermost);
        nextInner.Dispose();
        outer.Dispose();
        using CommandExecutionScope.Lease nextOuter = scope.Enter();
        Assert.True(nextOuter.IsOutermost);
    }

    [Fact]
    public async Task AtomicCommand_CommitsBeforeFlushingAndDoesNotSaveTwiceAsync() {
        var order = new List<string>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        IPostCommitActionQueue queue = Substitute.For<IPostCommitActionQueue>();
        queue.HasActions.Returns(returnThis: true);
        queue.FlushAsync(Arg.Any<CancellationToken>()).Returns(_ => { order.Add("flush"); return Task.CompletedTask; });
        IAtomicCommandExecutor executor = Substitute.For<IAtomicCommandExecutor>();
        executor.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<Result<string>>>>(), Arg.Any<CancellationToken>())
            .Returns(async call => {
                order.Add("begin");
                Result<string> response = await call.Arg<Func<CancellationToken, Task<Result<string>>>>()(call.Arg<CancellationToken>());
                order.Add("commit");
                return response;
            });
        var behavior = new CommandTransactionBehavior<AtomicCommand, Result<string>>(unitOfWork, queue, executor);
        Result<string> result = await behavior.Handle(new AtomicCommand(), _ => {
            order.Add("handler");
            return Task.FromResult(Result.Success("ok"));
        }, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(["begin", "handler", "commit", "flush"], order);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AtomicCommand_WithoutExecutorFailsBeforeHandlerAsync() {
        var behavior = new CommandTransactionBehavior<AtomicCommand, Result<string>>(
            Substitute.For<IUnitOfWork>(), Substitute.For<IPostCommitActionQueue>());
        bool invoked = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new AtomicCommand(), _ => {
            invoked = true;
            return Task.FromResult(Result.Success("ok"));
        }, CancellationToken.None));
        Assert.False(invoked);
    }

    [ExcludeFromCodeCoverage]
    private sealed record AtomicCommand : ICommand<Result<string>>, IAtomicCommand;

    [ExcludeFromCodeCoverage]
    private sealed record IdempotentCommand(Guid? UserId, AtomicOperation? Operation)
        : ICommand<Result<string>>, IIdempotentAtomicCommand, IUserRequest;
}
