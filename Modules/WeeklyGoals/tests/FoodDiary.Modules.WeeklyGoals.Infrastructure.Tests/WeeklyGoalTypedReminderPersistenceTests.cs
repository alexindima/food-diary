using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.WeeklyGoals.Infrastructure.Tests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalTypedReminderPersistenceTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TypedReminder_RoundTripsAndDisablesThroughExistingScalarColumnsAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"typed-weekly-goal-{Guid.NewGuid():N}@example.com", "hash");
        var week = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var goal = WeeklyGoal.CreateWithReminder(user.Id, week, WeeklyGoalType.DiaryLogging, 5,
            WeeklyGoalReminderSettings.EnabledAt(new TimeOnly(0, 15), TimeSpan.FromMinutes(345)));
        goal.MarkReminderSent(new DateOnly(2026, 8, 10), week.AddDays(-1).AddHours(19));
        context.AddRange(user, goal);
        Assert.False(context.Database.HasPendingModelChanges());

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        WeeklyGoal loaded = await context.WeeklyGoals.SingleAsync(value => value.Id == goal.Id);
        Assert.Multiple(() => {
            Assert.True(loaded.ReminderEnabled);
            Assert.Equal(15, loaded.ReminderTimeMinutes);
            Assert.Equal(345, loaded.TimeZoneOffsetMinutes);
            Assert.Equal(new DateOnly(2026, 8, 10), loaded.LastReminderLocalDate);
        });
        loaded.UpdateWithReminder(7, WeeklyGoalReminderSettings.Disabled, week.AddHours(1));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        WeeklyGoal reloaded = await context.WeeklyGoals.SingleAsync(value => value.Id == goal.Id);

        Assert.Multiple(() => {
            Assert.Equal(7, reloaded.TargetDays);
            Assert.False(reloaded.ReminderEnabled);
            Assert.Null(reloaded.ReminderTimeMinutes);
            Assert.Null(reloaded.TimeZoneOffsetMinutes);
            Assert.Null(reloaded.LastReminderLocalDate);
            Assert.Equal(week.AddHours(1), reloaded.ModifiedOnUtc);
        });
    }
}
