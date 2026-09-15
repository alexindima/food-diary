using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class EfModuleTransactionCoordinatorTests {
    [Theory]
    [InlineData("success")]
    [InlineData("result")]
    [InlineData("exception")]
    [InlineData("cancellation")]
    public async Task SerializableNonrelationalAttempt_PreservesSaveAndResetBehaviorAsync(string outcome) {
        await using FoodDiaryDbContext context = CreateContext();
        IPostCommitActionQueue queue = Substitute.For<IPostCommitActionQueue>();
        var unitOfWork = new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance);
        var coordinator = new EfModuleTransactionCoordinator(context, unitOfWork, queue);
        using var cancellation = new CancellationTokenSource();
        int attempts = 0;
        async Task<Result<int>> OperationAsync(CancellationToken token) {
            attempts++;
            context.Users.Add(User.Create("coordinator@example.com", "hash"));
            if (outcome.Equals("exception", StringComparison.Ordinal)) { throw new InvalidOperationException("Injected failure."); }
            if (outcome.Equals("cancellation", StringComparison.Ordinal)) {
                await cancellation.CancelAsync();
                token.ThrowIfCancellationRequested();
            }
            return outcome.Equals("result", StringComparison.Ordinal)
                ? Result.Failure<int>(new Error("Test.Failure", "Injected failure.", ErrorKind.Conflict)) : Result.Success(42);
        }
        if (outcome.Equals("exception", StringComparison.Ordinal)) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteSerializableAsync(OperationAsync, cancellation.Token));
        } else if (outcome.Equals("cancellation", StringComparison.Ordinal)) {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => coordinator.ExecuteSerializableAsync(OperationAsync, cancellation.Token));
        } else {
            Result<int> result = await coordinator.ExecuteSerializableAsync(OperationAsync, cancellation.Token);
            Assert.Equal(outcome.Equals("success", StringComparison.Ordinal), result.IsSuccess);
        }
        bool succeeded = outcome.Equals("success", StringComparison.Ordinal);
        Assert.Equal(1, attempts);
        Assert.Equal(succeeded, await context.Users.AsNoTracking().AnyAsync());
        Assert.False(context.ChangeTracker.HasChanges());
        if (!succeeded) { Assert.Empty(context.ChangeTracker.Entries()); }
        queue.Received(succeeded ? 1 : 2).Discard();
    }

    [Fact]
    public async Task SerializableNonrelationalAttempt_RejectsDirtyCallerWithoutDiscardingItAsync() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create("pending@example.com", "hash");
        context.Users.Add(user);
        IPostCommitActionQueue queue = Substitute.For<IPostCommitActionQueue>();
        var coordinator = new EfModuleTransactionCoordinator(context, Substitute.For<IUnitOfWork>(), queue);
        bool called = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteSerializableAsync(_ => {
            called = true;
            return Task.FromResult(true);
        }));
        Assert.False(called);
        Assert.Equal(EntityState.Added, context.Entry(user).State);
        queue.DidNotReceive().Discard();
    }

    private static FoodDiaryDbContext CreateContext() => new(new DbContextOptionsBuilder<FoodDiaryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
