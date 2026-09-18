using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;

namespace FoodDiary.Modules.Admin.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AdminRetentionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Retention_UsesEntryCreationDeduplicatesActivityAndExcludesImmatureCohorts() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        DateTime start = new(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var active = User.Create($"retention-active-{Guid.NewGuid():N}@example.com", "hash");
        var inactive = User.Create($"retention-inactive-{Guid.NewGuid():N}@example.com", "hash");
        var recent = User.Create($"retention-recent-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(active, inactive, recent);
        context.Entry(active).Property(user => user.CreatedOnUtc).CurrentValue = start;
        context.Entry(inactive).Property(user => user.CreatedOnUtc).CurrentValue = start;
        context.Entry(recent).Property(user => user.CreatedOnUtc).CurrentValue = start.AddDays(31);
        foreach (int day in new[] { 1, 1, 7, 30, 32 }) {
            // A backdated diary date is not the day the user returned to create the record.
            var meal = Meal.Create(active.Id, start.AddYears(-1));
            context.Meals.Add(meal);
            context.Entry(meal).Property(item => item.CreatedOnUtc).CurrentValue = start.AddDays(day).AddHours(1);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new AdminRetentionReader(context);

        AdminRetentionReport result = await reader.GetAsync(start, start.AddDays(32), start.AddDays(31).AddHours(12), CancellationToken.None);

        AdminRetentionCohort mature = result.Cohorts[0];
        Assert.Equal(2, mature.Registered);
        Assert.Equal(1, mature.ActivatedWithinSevenDays);
        Assert.Equal(1, mature.Day1);
        Assert.Equal(1, mature.Day7);
        Assert.Equal(1, mature.Day30);
        AdminRetentionCohort immature = result.Cohorts[1];
        Assert.Null(immature.ActivatedWithinSevenDays);
        Assert.Null(immature.Day1);
        Assert.Null(immature.Day7);
        Assert.Null(immature.Day30);
        Assert.Equal(1, result.ActiveUsersInPeriod);
        Assert.Equal(4, result.MealEntriesInPeriod);
        Assert.Equal(32, result.ActivityByDay.Count);
        Assert.Equal(0, result.ActivityByDay[0].ActiveUsers);
        Assert.Equal(0, result.ActivityByDay[0].MealEntries);
        Assert.Equal(1, result.ActivityByDay[1].ActiveUsers);
        Assert.Equal(2, result.ActivityByDay[1].MealEntries);
        Assert.Equal(0, result.ActivityByDay[^1].MealEntries);
        Assert.Equal(start, result.CohortFromUtc);

        // Activity includes older accounts even when the selected registration cohort is recent.
        AdminRetentionReport separate = await reader.GetAsync(start.AddDays(7), start.AddDays(9),
            start.AddDays(31).AddHours(12), CancellationToken.None, start.AddDays(31), start.AddDays(32));
        Assert.Single(separate.Cohorts);
        Assert.Equal(start.AddDays(31), separate.Cohorts[0].Date);
        Assert.Equal(1, separate.ActiveUsersInPeriod);
        Assert.Equal(1, separate.MealEntriesInPeriod);
        Assert.Equal(2, separate.ActivityByDay.Count);
        Assert.Equal(0, separate.ActivityByDay[1].ActiveUsers);
        Assert.Null(separate.Cohorts[0].Day30);

        AdminRetentionReport all = await reader.GetAsync(DateTime.UnixEpoch, start.AddDays(32),
            start.AddDays(31).AddHours(12), CancellationToken.None);
        Assert.Equal(start.AddDays(1), all.ActivityByDay[0].Date);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
