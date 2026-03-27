using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class UserRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetByEmailAsync_ReturnsActiveNonDeletedUserWithRoles() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var supportRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Support);
        var activeUser = User.Create("active@example.com", "hash");
        context.Users.Add(activeUser);
        context.UserRoles.AddRange(
            new UserRole(activeUser.Id, premiumRole.Id),
            new UserRole(activeUser.Id, supportRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        var loaded = await repository.GetByEmailAsync("active@example.com");

        Assert.NotNull(loaded);
        Assert.Equal(activeUser.Id, loaded.Id);
        Assert.Equal(2, loaded.UserRoles.Count);
        Assert.Contains(loaded.UserRoles, role => role.Role.Name == RoleNames.Premium);
        Assert.Contains(loaded.UserRoles, role => role.Role.Name == RoleNames.Support);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_NormalizesPagingAndEscapesLikePattern() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var matchingUser = User.Create("100%real@example.com", "hash");
        matchingUser.UpdateProfile(new FoodDiary.Domain.ValueObjects.UserProfileUpdate(Username: "special_user"));
        var otherUser = User.Create("1000real@example.com", "hash");
        otherUser.UpdateProfile(new FoodDiary.Domain.ValueObjects.UserProfileUpdate(Username: "plain_user"));
        context.Users.AddRange(matchingUser, otherUser);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        var (items, totalItems) = await repository.GetPagedAsync(
            search: "100%real",
            page: 0,
            limit: 0,
            includeDeleted: false);

        var item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingUser.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetAdminDashboardSummaryAsync_CountsPremiumAndSkipsDeletedInRecentUsers() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var firstUser = User.Create("first@example.com", "hash");
        var premiumUser = User.Create("premium@example.com", "hash");
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow);

        context.Users.AddRange(firstUser, premiumUser, deletedUser);
        context.UserRoles.Add(new UserRole(premiumUser.Id, premiumRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        var summary = await repository.GetAdminDashboardSummaryAsync(recentLimit: 10);

        Assert.Equal(4, summary.TotalUsers);
        Assert.Equal(3, summary.ActiveUsers);
        Assert.Equal(2, summary.PremiumUsers);
        Assert.Equal(1, summary.DeletedUsers);
        Assert.Equal(3, summary.RecentUsers.Count);
        Assert.DoesNotContain(summary.RecentUsers, user => user.Id == deletedUser.Id);
    }
}
