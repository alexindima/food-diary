using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class UserLoginEventRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DeleteOlderThanAsync_DeletesOnlyExpiredEventsWithinBatch() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"login-delete-{Guid.NewGuid():N}@example.com", "hash");
        var cutoffUtc = new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc);
        var oldest = CreateLoginEvent(user, cutoffUtc.AddDays(-3), "Chrome", "Windows", "Desktop");
        var older = CreateLoginEvent(user, cutoffUtc.AddDays(-2), "Firefox", "Linux", "Desktop");
        var fresh = CreateLoginEvent(user, cutoffUtc.AddMinutes(-1), "Safari", "iOS", "Mobile");
        context.Users.Add(user);
        context.UserLoginEvents.AddRange(oldest, older, fresh);
        await context.SaveChangesAsync();

        var repository = new UserLoginEventRepository(context);

        var firstDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 1);
        var secondDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 10);

        Assert.Equal(1, firstDeletedCount);
        Assert.Equal(1, secondDeletedCount);
        var remaining = await context.UserLoginEvents.AsNoTracking().SingleAsync();
        Assert.Equal(fresh.Id, remaining.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_FiltersSearchAndReturnsUserMetadata() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var matchingUser = User.Create($"login-search-{Guid.NewGuid():N}@example.com", "hash");
        var otherUser = User.Create($"login-other-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(matchingUser, otherUser);
        context.UserLoginEvents.AddRange(
            CreateLoginEvent(matchingUser, new DateTime(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc), "Chrome", "Windows", "Desktop"),
            CreateLoginEvent(otherUser, new DateTime(2030, 3, 28, 13, 0, 0, DateTimeKind.Utc), "Safari", "iOS", "Mobile"));
        await context.SaveChangesAsync();

        var repository = new UserLoginEventRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            page: 1,
            limit: 10,
            userId: null,
            search: "windows");

        var item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingUser.Id.Value, item.UserId);
        Assert.Equal(matchingUser.Email, item.UserEmail);
        Assert.Equal("Windows", item.OperatingSystem);
        Assert.Equal("Chrome", item.BrowserName);
    }

    [RequiresDockerFact]
    public async Task GetDeviceSummaryAsync_AggregatesWithinDateRange() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"login-summary-{Guid.NewGuid():N}@example.com", "hash");
        var fromUtc = new DateTime(2030, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = new DateTime(2030, 3, 31, 23, 59, 59, DateTimeKind.Utc);
        var latestDesktopAtUtc = new DateTime(2030, 3, 20, 12, 0, 0, DateTimeKind.Utc);
        context.Users.Add(user);
        context.UserLoginEvents.AddRange(
            CreateLoginEvent(user, new DateTime(2030, 2, 28, 12, 0, 0, DateTimeKind.Utc), "Chrome", "Windows", "Desktop"),
            CreateLoginEvent(user, new DateTime(2030, 3, 10, 12, 0, 0, DateTimeKind.Utc), "Chrome", "Windows", "Desktop"),
            CreateLoginEvent(user, latestDesktopAtUtc, "Chrome", "Windows", "Desktop"),
            CreateLoginEvent(user, new DateTime(2030, 3, 15, 12, 0, 0, DateTimeKind.Utc), "Safari", "iOS", "Mobile"));
        await context.SaveChangesAsync();

        var repository = new UserLoginEventRepository(context);

        var summary = await repository.GetDeviceSummaryAsync(fromUtc, toUtc);

        var desktop = Assert.Single(summary, item => item.Key == "device:Desktop");
        Assert.Equal(2, desktop.Count);
        Assert.Equal(latestDesktopAtUtc, desktop.LastSeenAtUtc);
        Assert.Contains(summary, item => item.Key == "browser:Chrome" && item.Count == 2);
        Assert.Contains(summary, item => item.Key == "os:iOS" && item.Count == 1);
        Assert.DoesNotContain(summary, item => item.LastSeenAtUtc < fromUtc || item.LastSeenAtUtc > toUtc);
    }

    private static UserLoginEvent CreateLoginEvent(
        User user,
        DateTime loggedInAtUtc,
        string browserName,
        string operatingSystem,
        string deviceType) =>
        UserLoginEvent.Create(
            user.Id,
            "password",
            "203.0.113.42",
            $"Mozilla/5.0 {browserName}",
            browserName,
            "125.0",
            operatingSystem,
            deviceType,
            loggedInAtUtc);
}
