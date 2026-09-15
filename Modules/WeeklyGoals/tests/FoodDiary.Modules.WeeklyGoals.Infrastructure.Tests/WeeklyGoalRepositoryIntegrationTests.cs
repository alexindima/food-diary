using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.WeeklyGoals.Infrastructure;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Repository_AddsReadsTracksAndFindsReminderCandidates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"weekly-goal-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(context);
        IWeeklyGoalRepository repository = provider.GetRequiredService<IWeeklyGoalRepository>();
        WeeklyGoalsDbContext owned = provider.GetRequiredService<WeeklyGoalsDbContext>();
        var weekStartUtc = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var goal = WeeklyGoal.Create(
            user.Id,
            weekStartUtc,
            WeeklyGoalType.DiaryLogging,
            targetDays: 5,
            reminderEnabled: true,
            reminderTimeMinutes: 570,
            timeZoneOffsetMinutes: 240);

        await repository.AddAsync(goal, CancellationToken.None);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        owned.ChangeTracker.Clear();

        WeeklyGoal? untracked = await repository.GetAsync(user.Id, weekStartUtc, cancellationToken: CancellationToken.None);
        Assert.NotNull(untracked);
        Assert.Equal(EntityState.Detached, owned.Entry(untracked).State);

        WeeklyGoal? tracked = await repository.GetAsync(
            user.Id,
            weekStartUtc,
            asTracking: true,
            cancellationToken: CancellationToken.None);
        IReadOnlyList<WeeklyGoal> reminders = await repository.GetReminderCandidatesAsync(
            weekStartUtc.AddDays(-7),
            weekStartUtc.AddDays(7),
            offset: 0,
            limit: 10,
            CancellationToken.None);

        Assert.NotNull(tracked);
        Assert.Multiple(
            () => Assert.Equal(EntityState.Unchanged, owned.Entry(tracked).State),
            () => Assert.Equal(goal.Id, tracked.Id),
            () => Assert.Equal(goal.Id, Assert.Single(reminders).Id));
    }

    [RequiresDockerFact]
    public async Task TransactionRunner_SerializesConcurrentCreationForSameUserAndWeek() {
        await using FoodDiaryDbContext setupContext = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"weekly-goal-race-{Guid.NewGuid():N}@example.com", "hash");
        setupContext.Users.Add(user);
        await setupContext.SaveChangesAsync();
        var weekStartUtc = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);
        string connectionString = setupContext.Database.GetConnectionString()!;

        await using FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(connectionString);

        Task first = CreateGoalWithRunnerAsync(firstContext, user.Id, weekStartUtc);
        Task second = CreateGoalWithRunnerAsync(secondContext, user.Id, weekStartUtc);
        await Task.WhenAll(first, second);

        await using FoodDiaryDbContext verificationContext = databaseFixture.CreateDbContext(connectionString);
        Assert.Equal(1, await verificationContext.WeeklyGoals.CountAsync(
            goal => goal.UserId == user.Id && goal.WeekStartUtc == weekStartUtc));
    }

    [RequiresDockerFact]
    public async Task FailedSerializedAttemptClearsOwnerChangesAndCanBeRetriedAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"weekly-retry-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        await using ServiceProvider provider = CreateProvider(context);
        IWeeklyGoalRepository repository = provider.GetRequiredService<IWeeklyGoalRepository>();
        WeeklyGoalsDbContext owned = provider.GetRequiredService<WeeklyGoalsDbContext>();
        IWeeklyGoalTransactionRunner runner = provider.GetRequiredService<IWeeklyGoalTransactionRunner>();
        var week = new DateTime(2026, 8, 17, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(typeof(WeeklyGoal), Assert.Single(owned.Model.GetEntityTypes()).ClrType);
        Assert.Same(context.Database.GetDbConnection(), owned.Database.GetDbConnection());
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteSerializedAsync<bool>(user.Id, week, async token => {
            await repository.AddAsync(WeeklyGoal.Create(user.Id, week, WeeklyGoalType.DiaryLogging, targetDays: 5, reminderEnabled: false, reminderTimeMinutes: null, timeZoneOffsetMinutes: null), token);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(token);
            throw new InvalidOperationException("Injected failure after owner save.");
        }));
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.Null(await repository.GetAsync(user.Id, week));
        await runner.ExecuteSerializedAsync(user.Id, week, async token => {
            await repository.AddAsync(WeeklyGoal.Create(user.Id, week, WeeklyGoalType.DiaryLogging, targetDays: 5, reminderEnabled: false, reminderTimeMinutes: null, timeZoneOffsetMinutes: null), token);
            return true;
        });
        Assert.NotNull(await repository.GetAsync(user.Id, week));
        Assert.Empty(context.ChangeTracker.Entries<WeeklyGoal>());
    }

    private static async Task CreateGoalWithRunnerAsync(
        FoodDiaryDbContext context,
        FoodDiary.Domain.ValueObjects.Ids.UserId userId,
        DateTime weekStartUtc) {
        await using ServiceProvider provider = CreateProvider(context);
        IWeeklyGoalRepository repository = provider.GetRequiredService<IWeeklyGoalRepository>();
        IWeeklyGoalTransactionRunner runner = provider.GetRequiredService<IWeeklyGoalTransactionRunner>();
        await runner.ExecuteSerializedAsync(
            userId,
            weekStartUtc,
            async cancellationToken => {
                WeeklyGoal? goal = await repository.GetAsync(
                    userId, weekStartUtc, asTracking: true, cancellationToken);
                if (goal is null) {
                    await repository.AddAsync(
                        WeeklyGoal.Create(
                            userId,
                            weekStartUtc,
                            WeeklyGoalType.DiaryLogging,
                            targetDays: 5,
                            reminderEnabled: false,
                            reminderTimeMinutes: null,
                            timeZoneOffsetMinutes: null),
                        cancellationToken);
                }

                return true;
            },
            CancellationToken.None);
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddWeeklyGoalsModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
