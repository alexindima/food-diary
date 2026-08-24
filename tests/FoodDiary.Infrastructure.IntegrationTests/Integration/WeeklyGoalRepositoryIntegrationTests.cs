using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.WeeklyGoals;
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
        var repository = new WeeklyGoalRepository(context);
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
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        WeeklyGoal? untracked = await repository.GetAsync(user.Id, weekStartUtc, cancellationToken: CancellationToken.None);
        Assert.NotNull(untracked);
        Assert.Equal(EntityState.Detached, context.Entry(untracked).State);

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
            () => Assert.Equal(EntityState.Unchanged, context.Entry(tracked).State),
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

    private static async Task CreateGoalWithRunnerAsync(
        FoodDiaryDbContext context,
        FoodDiary.Domain.ValueObjects.Ids.UserId userId,
        DateTime weekStartUtc) {
        var repository = new WeeklyGoalRepository(context);
        var runner = new EfWeeklyGoalTransactionRunner(context, new TestUnitOfWork(context));
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

    [ExcludeFromCodeCoverage]
    private sealed class TestUnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
        public bool HasPendingChanges => context.ChangeTracker.HasChanges();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            context.SaveChangesAsync(cancellationToken);
    }
}
