using FoodDiary.Infrastructure;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;

using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalTransactionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime Week = new(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);

    [RequiresDockerTheory]
    [InlineData("result")]
    [InlineData("exception")]
    [InlineData("cancellation")]
    public async Task FailedAttempt_RollsBackBothContextsAndDiscardsCallbacks(string failure) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"weekly-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var queue = new RecordingActionQueue();
        await using ServiceProvider provider = CreateProvider(context, queue);
        IWeeklyGoalRepository repository = provider.GetRequiredService<IWeeklyGoalRepository>();
        WeeklyGoalsDbContext owner = provider.GetRequiredService<WeeklyGoalsDbContext>();
        IWeeklyGoalTransactionRunner runner = provider.GetRequiredService<IWeeklyGoalTransactionRunner>();
        IModuleTransactionCoordinator coordinator = provider.GetRequiredService<IModuleTransactionCoordinator>();
        var uncommitted = User.Create($"rollback-{Guid.NewGuid():N}@example.com", "hash");
        int delivered = 0;
        using var cancellation = new CancellationTokenSource();
        async Task<Result> MutateAsync(CancellationToken token) {
            Assert.NotNull(coordinator.CurrentTransaction);
            Assert.Null(await repository.GetAsync(user.Id, Week, cancellationToken: token));
            context.Users.Add(uncommitted);
            await repository.AddAsync(CreateGoal(user.Id), token);
            queue.Enqueue("notification", _ => { delivered++; return Task.CompletedTask; });
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);
            if (failure.Equals("exception", StringComparison.Ordinal)) { throw new InvalidOperationException("Injected failure after save."); }
            if (failure.Equals("cancellation", StringComparison.Ordinal)) {
                await cancellation.CancelAsync();
                token.ThrowIfCancellationRequested();
            }
            return Result.Failure<int>(new Error("WeeklyGoals.TestFailure", "Injected failure", ErrorKind.Conflict));
        }

        if (failure.Equals("result", StringComparison.Ordinal)) {
            Result result = await runner.ExecuteSerializedAsync(user.Id, Week, MutateAsync, cancellation.Token);
            Assert.True(result.IsFailure);
        } else if (failure.Equals("exception", StringComparison.Ordinal)) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteSerializedAsync(user.Id, Week, MutateAsync, cancellation.Token));
        } else {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runner.ExecuteSerializedAsync(user.Id, Week, MutateAsync, cancellation.Token));
        }
        Assert.Multiple(
            () => Assert.Empty(context.ChangeTracker.Entries()),
            () => Assert.Empty(owner.ChangeTracker.Entries()),
            () => Assert.Null(coordinator.CurrentTransaction),
            () => Assert.False(queue.HasActions));
        Assert.False(await context.Users.AnyAsync(candidate => candidate.Id == uncommitted.Id));
        Assert.Null(await repository.GetAsync(user.Id, Week));
        await queue.FlushAsync();
        Assert.Equal(0, delivered);

        await runner.ExecuteSerializedAsync(user.Id, Week, async token => {
            await repository.AddAsync(CreateGoal(user.Id), token);
            queue.Enqueue("notification", _ => { delivered++; return Task.CompletedTask; });
            return true;
        });
        Assert.NotNull(await repository.GetAsync(user.Id, Week));
        Assert.Equal(0, delivered);
        await queue.FlushAsync();
        Assert.Equal(1, delivered);
    }

    [RequiresDockerTheory]
    [InlineData("owner")]
    [InlineData("shared")]
    [InlineData("queue")]
    [InlineData("nested")]
    public async Task DirtyEntry_IsRejectedWithoutDiscardingCallerState(string pending) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var queue = new RecordingActionQueue();
        await using ServiceProvider provider = CreateProvider(context, queue);
        IWeeklyGoalRepository repository = provider.GetRequiredService<IWeeklyGoalRepository>();
        WeeklyGoalsDbContext owner = provider.GetRequiredService<WeeklyGoalsDbContext>();
        if (pending.Equals("owner", StringComparison.Ordinal)) { await repository.AddAsync(CreateGoal(UserId.New())); }
        if (pending.Equals("shared", StringComparison.Ordinal)) { context.Users.Add(User.Create("pending@example.com", "hash")); }
        if (pending.Equals("queue", StringComparison.Ordinal)) { queue.Enqueue("existing", _ => Task.CompletedTask); }
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? outer = pending.Equals("nested", StringComparison.Ordinal)
            ? await context.Database.BeginTransactionAsync() : null;
        bool called = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRequiredService<IWeeklyGoalTransactionRunner>()
            .ExecuteSerializedAsync(UserId.New(), Week, _ => { called = true; return Task.FromResult(true); }));
        Assert.False(called);
        Assert.Equal(pending.Equals("owner", StringComparison.Ordinal), owner.ChangeTracker.HasChanges());
        Assert.Equal(pending.Equals("shared", StringComparison.Ordinal), context.ChangeTracker.HasChanges());
        Assert.Equal(pending.Equals("queue", StringComparison.Ordinal), queue.HasActions);
        Assert.Same(outer, context.Database.CurrentTransaction);
    }

    private static WeeklyGoal CreateGoal(UserId userId) => WeeklyGoal.Create(userId, Week, WeeklyGoalType.DiaryLogging,
        targetDays: 5, reminderEnabled: false, reminderTimeMinutes: null, timeZoneOffsetMinutes: null);

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context, IPostCommitActionQueue queue) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton(queue);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddWeeklyGoalsModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingActionQueue : IPostCommitActionQueue {
        private readonly List<Func<CancellationToken, Task>> _actions = [];
        public bool HasActions => _actions.Count > 0;
        public void Discard() => _actions.Clear();
        public void Enqueue(string actionName, Func<CancellationToken, Task> action) => _actions.Add(action);
        public async Task FlushAsync(CancellationToken cancellationToken = default) {
            foreach (Func<CancellationToken, Task> action in _actions) { await action(cancellationToken); }
            _actions.Clear();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
