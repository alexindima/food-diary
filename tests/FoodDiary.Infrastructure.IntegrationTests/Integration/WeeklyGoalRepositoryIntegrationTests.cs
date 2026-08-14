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
            limit: 10,
            CancellationToken.None);

        Assert.NotNull(tracked);
        Assert.Multiple(
            () => Assert.Equal(EntityState.Unchanged, context.Entry(tracked).State),
            () => Assert.Equal(goal.Id, tracked.Id),
            () => Assert.Equal(goal.Id, Assert.Single(reminders).Id));
    }
}
