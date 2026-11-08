using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserActivityRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ConcurrentFirstActivity_UpsertsOneRowAndPreservesTheLatestInstant() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("concurrent-activity@example.com", "hash");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        string connection = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext first = databaseFixture.CreateDbContext(connection);
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(connection);
        DateTime now = DateTime.UtcNow;
        await Task.WhenAll(
            new UserActivityRepository(first.Database).RecordAsync(user.Id, now, CancellationToken.None),
            new UserActivityRepository(second.Database).RecordAsync(user.Id, now.AddMinutes(5), CancellationToken.None));
        UserActivity activity = await seed.Set<UserActivity>().SingleAsync();
        Assert.Equal(now.AddMinutes(5).Ticks / 10, activity.LastActiveAtUtc.Ticks / 10);
    }

    [RequiresDockerFact]
    public async Task Activity_IsThrottledMonotonicAndIndependentOfAccountEdits() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("activity@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        uint version = context.Entry(user).Property<uint>("xmin").CurrentValue;
        DateTime? modified = user.ModifiedOnUtc;
        var now = new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var repository = new UserActivityRepository(context.Database);
        await repository.RecordAsync(user.Id, now, CancellationToken.None);
        await repository.RecordAsync(user.Id, now.AddMinutes(4), CancellationToken.None);
        await repository.RecordAsync(user.Id, now.AddMinutes(-10), CancellationToken.None);
        Assert.Equal(now, await ReadActivityAsync());
        await repository.RecordAsync(user.Id, now.AddMinutes(5), CancellationToken.None);
        Assert.Equal(now.AddMinutes(5), await ReadActivityAsync());
        // A foreground ping must not invalidate an already loaded account edit.
        user.SetEmailConfirmed(isConfirmed: true);
        await context.SaveChangesAsync();
        await context.Entry(user).ReloadAsync();
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(now.AddMinutes(5), await ReadActivityAsync(verifyAccount: false));

        async Task<DateTime> ReadActivityAsync(bool verifyAccount = true) {
            await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
            User account = await read.Users.Include(candidate => candidate.Activity).SingleAsync();
            if (verifyAccount) {
                Assert.Multiple(
                    () => Assert.Equal(version, read.Entry(account).Property<uint>("xmin").CurrentValue),
                    () => Assert.Equal(modified, account.ModifiedOnUtc),
                    () => Assert.Null(account.LastLoginAtUtc),
                    () => Assert.Equal(0, account.SecurityVersion));
            }
            return Assert.IsType<UserActivity>(account.Activity).LastActiveAtUtc;
        }
    }

    [RequiresDockerFact]
    public async Task Activity_RejectsInactiveAndDeletedUsersAndCascadesOnPurge() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("active-activity@example.com", "hash");
        var inactive = User.Create("inactive-activity@example.com", "hash");
        inactive.Deactivate();
        var deleted = User.Create("deleted-activity@example.com", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(active, inactive, deleted);
        await context.SaveChangesAsync();
        var repository = new UserActivityRepository(context.Database);
        DateTime now = DateTime.UtcNow;
        foreach (User account in new[] { active, inactive, deleted }) {
            await repository.RecordAsync(account.Id, now, CancellationToken.None);
        }
        Assert.Equal(active.Id, (await context.Set<UserActivity>().SingleAsync()).UserId);
        context.Users.Remove(active);
        await context.SaveChangesAsync();
        Assert.Empty(await context.Set<UserActivity>().ToListAsync());
    }
}
