using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

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
        foreach (int day in new[] { 1, 1, 7, 30 }) {
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
        Assert.All(result.ActivityByDay, day => Assert.Equal(1, day.ActiveUsers));
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
