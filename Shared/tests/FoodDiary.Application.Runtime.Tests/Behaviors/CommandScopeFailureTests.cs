using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Application.Runtime.Common.Services;
using FoodDiary.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Runtime.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public sealed class CommandScopeFailureTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOuterCommand_CannotLeakChangesOrCallbacksIntoNextCommand(bool throws) {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        bool pending = false;
        unitOfWork.HasPendingChanges.Returns(_ => pending);
        unitOfWork.When(work => work.DiscardChanges()).Do(_ => pending = false);
        var actions = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance, TimeProvider.System);
        bool delivered = false;
        var behavior = new CommandTransactionBehavior<ProbeCommand, Result>(unitOfWork, actions);
        Task<Result> failed = behavior.Handle(new ProbeCommand(), _ => {
            pending = true;
            actions.Enqueue("failed", _ => { delivered = true; return Task.CompletedTask; });
            return throws ? throw new InvalidOperationException("failed") : Task.FromResult(Result.Failure(new Error("Probe.Failed", "Failed")));
        }, CancellationToken.None);
        if (throws) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => failed);
        } else {
            ResultAssert.Failure(await failed);
        }
        ResultAssert.Success(await behavior.Handle(new ProbeCommand(), _ => Task.FromResult(Result.Success()), CancellationToken.None));
        Assert.False(delivered);
        Assert.False(pending);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NestedFailure_LeavesCleanupAndSaveOwnershipWithOuterCommand() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        IPostCommitActionQueue actions = Substitute.For<IPostCommitActionQueue>();
        var scope = new CommandExecutionScope();
        var outer = new CommandTransactionBehavior<ProbeCommand, Result>(unitOfWork, actions, executionScope: scope);
        var nested = new CommandTransactionBehavior<ProbeCommand, Result>(unitOfWork, actions, executionScope: scope);
        ResultAssert.Failure(await outer.Handle(new ProbeCommand(), async token => {
            Result result = await nested.Handle(new ProbeCommand(), _ => Task.FromResult(Result.Failure(new Error("Probe.Failed", "Failed"))), token);
            unitOfWork.DidNotReceive().DiscardChanges();
            await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
            return result;
        }, CancellationToken.None));
        unitOfWork.Received(1).DiscardChanges();
        actions.Received(1).Discard();
    }

    [Fact]
    public async Task IgnoredNestedFailure_PreventsTheOuterCommit() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        IPostCommitActionQueue actions = Substitute.For<IPostCommitActionQueue>();
        var scope = new CommandExecutionScope();
        var outer = new CommandTransactionBehavior<ProbeCommand, Result>(unitOfWork, actions, executionScope: scope);
        var nested = new CommandTransactionBehavior<ProbeCommand, Result>(unitOfWork, actions, executionScope: scope);
        await Assert.ThrowsAsync<InvalidOperationException>(() => outer.Handle(new ProbeCommand(), async token => {
            await nested.Handle(new ProbeCommand(), _ => Task.FromResult(Result.Failure(new Error("Probe.Failed", "Failed"))), token);
            return Result.Success();
        }, CancellationToken.None));
        unitOfWork.Received(1).DiscardChanges();
        actions.Received(1).Discard();
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed record ProbeCommand : ICommand<Result>;
}
