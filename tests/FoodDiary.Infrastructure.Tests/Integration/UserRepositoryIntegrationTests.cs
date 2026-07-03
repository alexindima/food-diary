using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetByEmailAsync_ReturnsActiveNonDeletedUserWithRoles() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        Role supportRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Support);
        var activeUser = User.Create("active@example.com", "hash");
        context.Users.Add(activeUser);
        context.UserRoles.AddRange(
            new UserRole(activeUser.Id, premiumRole.Id),
            new UserRole(activeUser.Id, supportRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        User? loaded = await repository.GetByEmailAsync("active@example.com");

        Assert.NotNull(loaded);
        Assert.Equal(activeUser.Id, loaded.Id);
        Assert.Equal(2, loaded.UserRoles.Count);
        Assert.Contains(loaded.UserRoles, role => string.Equals(role.Role.Name, RoleNames.Premium, StringComparison.Ordinal));
        Assert.Contains(loaded.UserRoles, role => string.Equals(role.Role.Name, RoleNames.Support, StringComparison.Ordinal));
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_NormalizesPagingAndEscapesLikePattern() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var matchingUser = User.Create("100%real@example.com", "hash");
        matchingUser.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "special_user"));
        var otherUser = User.Create("1000real@example.com", "hash");
        otherUser.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "plain_user"));
        context.Users.AddRange(matchingUser, otherUser);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        (IReadOnlyList<User>? items, int totalItems) = await repository.GetPagedAsync(
            search: "100%real",
            page: 0,
            limit: 0,
            includeDeleted: false);

        User item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Equal(matchingUser.Id, item.Id);
    }

    [RequiresDockerFact]
    public async Task GetPagedAsync_ReturnsPagedUsersWithRolesLoaded() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create($"paged-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole(user.Id, premiumRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        (IReadOnlyList<User>? items, int totalItems) = await repository.GetPagedAsync(
            search: user.Email,
            page: 1,
            limit: 10,
            includeDeleted: false);

        User item = Assert.Single(items);
        Assert.Equal(1, totalItems);
        Assert.Single(item.UserRoles);
        Assert.Equal(RoleNames.Premium, item.UserRoles.Single().Role.Name);
    }

    [RequiresDockerFact]
    public async Task GetAdminDashboardSummaryAsync_CountsPremiumAndSkipsDeletedInRecentUsers() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var firstUser = User.Create("first@example.com", "hash");
        var premiumUser = User.Create("premium@example.com", "hash");
        var deletedUser = User.Create("deleted@example.com", "hash");
        deletedUser.MarkDeleted(DateTime.UtcNow);

        context.Users.AddRange(firstUser, premiumUser, deletedUser);
        context.UserRoles.Add(new UserRole(premiumUser.Id, premiumRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        (int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers) = await repository.GetAdminDashboardSummaryAsync(recentLimit: 10);

        Assert.Equal(3, TotalUsers);
        Assert.Equal(2, ActiveUsers);
        Assert.Equal(1, PremiumUsers);
        Assert.Equal(1, DeletedUsers);
        Assert.Equal(2, RecentUsers.Count);
        Assert.DoesNotContain(RecentUsers, user => user.Id == deletedUser.Id);
    }

    [RequiresDockerFact]
    public async Task EnsureRoleAsync_IsIdempotentForExistingUserRole() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create($"billing-role-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        await repository.EnsureRoleAsync(user, RoleNames.Premium);
        await repository.EnsureRoleAsync(user, RoleNames.Premium);

        int roleCount = await context.UserRoles.CountAsync(userRole =>
            userRole.UserId == user.Id &&
            userRole.RoleId == premiumRole.Id);
        Assert.Equal(1, roleCount);
    }

    [RequiresDockerFact]
    public async Task UpdateAsync_WhenUserIsDetached_AttachesForUpdate() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"detached-update-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.Entry(user).State = EntityState.Detached;
        user.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "detached-user"));
        var repository = new UserRepository(context);

        await repository.UpdateAsync(user);

        Assert.Equal(EntityState.Modified, context.Entry(user).State);
    }

    [RequiresDockerFact]
    public async Task RemoveRoleAsync_IsIdempotentForMissingUserRole() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create($"billing-role-remove-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole(user.Id, premiumRole.Id));
        await context.SaveChangesAsync();

        var repository = new UserRepository(context);

        await repository.RemoveRoleAsync(user, RoleNames.Premium);
        await repository.RemoveRoleAsync(user, RoleNames.Premium);

        int roleCount = await context.UserRoles.CountAsync(userRole =>
            userRole.UserId == user.Id &&
            userRole.RoleId == premiumRole.Id);
        Assert.Equal(0, roleCount);
    }
}
