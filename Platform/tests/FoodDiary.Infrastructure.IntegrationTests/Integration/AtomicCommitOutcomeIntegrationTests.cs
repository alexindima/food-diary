using System.Data.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Meals.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Testing.Assertions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AtomicCommitOutcomeIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ReceiptMaintenance_BoundsExpiryAndParticipatesInOwnerPurgeTransaction() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = Guid.NewGuid();
        var otherOwner = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        context.AddRange(Enumerable.Range(0, 1002).Select(index => new AtomicCommandReceipt {
            Key = index.ToString("D64", System.Globalization.CultureInfo.InvariantCulture),
            UserId = otherOwner,
            RequestHash = new string('B', 64),
            ResponseType = "test",
            ResponseJson = "{}",
            ExpiresOnUtc = now.AddDays(-1),
        }));
        context.Add(new AtomicCommandReceipt {
            Key = new string('A', 64),
            UserId = owner,
            RequestHash = new string('B', 64),
            ResponseType = "test",
            ResponseJson = "{}",
            ExpiresOnUtc = now.AddDays(1),
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var maintenance = new AtomicCommandReceiptMaintenance(context, TimeProvider.System);
        Assert.Equal(1000, await maintenance.DeleteExpiredBatchAsync(10000));
        Assert.Equal(2, await maintenance.DeleteExpiredBatchAsync(10000));
        var coordinator = new EfModuleTransactionCoordinator(context,
            new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance), new RecordingActions());
        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteAsync<bool>(async (_, token) => {
            await maintenance.DeleteOwnerAsync(owner, token).ConfigureAwait(false);
            throw new InvalidOperationException("Purge failed before commit.");
        }));
        Assert.Equal(1, await context.Set<AtomicCommandReceipt>().CountAsync());
        await coordinator.ExecuteAsync(async (_, token) => {
            await maintenance.DeleteOwnerAsync(owner, token).ConfigureAwait(false);
            return true;
        });
        Assert.Empty(await context.Set<AtomicCommandReceipt>().ToListAsync());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulCommitWithLostAcknowledgement_DoesNotReplayHandlerOrLoseCallbacks(bool durableKey) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("ambiguous-commit@example.com", "hash");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        var fault = new LoseCommitAcknowledgement();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString(), provider => provider.EnableRetryOnFailure(2, TimeSpan.Zero, errorCodesToAdd: null))
            .AddInterceptors(fault).Options;
        await using var root = new FoodDiaryDbContext(options);
        await using MealsDbContext meals = root.CreateModuleContext<MealsDbContext>(options => new MealsDbContext(options));
        var actions = new RecordingActions();
        var coordinator = new EfModuleTransactionCoordinator(root,
            new EfUnitOfWork(root, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance), actions);
        var executor = new EfAtomicCommandExecutor(coordinator, root, TimeProvider.System);
        int invocations = 0;
        int callbacks = 0;
        Task<Result<Guid>> CreateAsync(CancellationToken token) {
            invocations++;
            var meal = Meal.Create(user.Id, DateTime.UtcNow);
            meals.Meals.Add(meal);
            actions.Enqueue("commit", _ => { callbacks++; return Task.CompletedTask; });
            return Task.FromResult(Result.Success(meal.Id.Value));
        }
        var identity = new AtomicOperation(user.Id.Value, new string('A', 64), new string('B', 64), TimeSpan.FromDays(1));
        Result<Guid> result = durableKey ? await executor.ExecuteAsync(identity, CreateAsync)
            : await executor.ExecuteAsync(CreateAsync);
        await actions.FlushAsync();
        Assert.Equal(1, invocations);
        Assert.Equal(1, callbacks);
        Assert.Equal(ResultAssert.Success(result), (await seed.Meals.AsNoTracking().SingleAsync()).Id.Value);
        if (durableKey) {
            root.ChangeTracker.Clear();
            meals.ChangeTracker.Clear();
            Result<Guid> replay = await executor.ExecuteAsync(identity, CreateAsync);
            Assert.Equal(result.Value, ResultAssert.Success(replay));
            Assert.Equal(1, invocations);
            Assert.False(actions.HasActions);
            Result<Guid> conflict = await executor.ExecuteAsync(identity with { RequestHash = new string('C', 64) }, CreateAsync);
            Assert.Equal("Idempotency.Conflict", ResultAssert.Failure(conflict).Code);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class LoseCommitAcknowledgement : DbTransactionInterceptor {
        private bool _lost;
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default) {
            if (_lost) {
                return Task.CompletedTask;
            }
            _lost = true;
            throw new TimeoutException("COMMIT completed but its acknowledgement was lost.");
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingActions : IPostCommitActionQueue {
        private readonly List<Func<CancellationToken, Task>> _actions = [];
        public bool HasActions => _actions.Count > 0;
        public void Enqueue(string actionName, Func<CancellationToken, Task> action) => _actions.Add(action);
        public void Discard() => _actions.Clear();
        public async Task FlushAsync(CancellationToken cancellationToken = default) {
            foreach (Func<CancellationToken, Task> action in _actions) {
                await action(cancellationToken);
            }
            _actions.Clear();
        }
    }
}
