using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Results;

namespace FoodDiary.Application.Runtime.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public sealed class AtomicCommandBehaviorTests {
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
}
