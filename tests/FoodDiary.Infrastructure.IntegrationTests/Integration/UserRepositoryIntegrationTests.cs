using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SaveChangesAsync_WithConcurrentUserUpdates_RejectsStaleWriter() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using (FoodDiaryDbContext setupContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true)) {
            await setupContext.Database.MigrateAsync();
            setupContext.Users.Add(User.Create("concurrency@example.com", "hash"));
            await setupContext.SaveChangesAsync();
        }

        await using FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        User firstCopy = await firstContext.Users.SingleAsync(user => user.Email == "concurrency@example.com");
        User staleCopy = await secondContext.Users.SingleAsync(user => user.Email == "concurrency@example.com");
        firstCopy.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "first-writer"));
        staleCopy.UpdatePersonalInfo(new FoodDiary.Domain.ValueObjects.UserPersonalInfoUpdate(Username: "stale-writer"));

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

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
    public async Task GetByEmailAsync_DoesNotLoadGoalHistoryNeededOnlyByAggregateWorkflows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"identity-query-{Guid.NewGuid():N}@example.com", "hash");
        DateTime startedAtUtc = DateTime.UtcNow;
        user.StartWeightGoal(70, 80, startedAtUtc);
        user.StartWaistGoal(80, 90, startedAtUtc);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        User? loaded = await repository.GetByEmailAsync(user.Email);

        Assert.NotNull(loaded);
        Assert.Multiple(
            () => Assert.Empty(loaded.WeightGoals),
            () => Assert.Empty(loaded.WaistGoals));
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_LoadsGoalHistoryRequiredByAggregateWorkflows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"aggregate-query-{Guid.NewGuid():N}@example.com", "hash");
        DateTime startedAtUtc = DateTime.UtcNow;
        user.StartWeightGoal(70, 80, startedAtUtc);
        user.StartWaistGoal(80, 90, startedAtUtc);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        User? loaded = await repository.GetByIdAsync(user.Id);

        Assert.NotNull(loaded);
        Assert.Multiple(
            () => Assert.Single(loaded.WeightGoals),
            () => Assert.Single(loaded.WaistGoals));
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
        context.ChangeTracker.Clear();

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
        Assert.Equal(EntityState.Detached, context.Entry(item).State);
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

        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<User> recentUsers) = await repository.GetAdminDashboardSummaryAsync(recentLimit: 10);

        Assert.Equal(3, totalUsers);
        Assert.Equal(2, activeUsers);
        Assert.Equal(1, premiumUsers);
        Assert.Equal(1, deletedUsers);
        Assert.Equal(2, recentUsers.Count);
        Assert.DoesNotContain(recentUsers, user => user.Id == deletedUser.Id);
    }

    [RequiresDockerFact]
    public async Task UserRoleMembershipService_EnsureRoleAsync_IsIdempotentForExistingUserRole() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create($"billing-role-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new UserRoleMembershipService(context);

        await service.EnsureRoleAsync(user.Id, RoleNames.Premium);
        await service.EnsureRoleAsync(user.Id, RoleNames.Premium);

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
    public async Task UserRoleMembershipService_RemoveRoleAsync_IsIdempotentForMissingUserRole() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role premiumRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var user = User.Create($"billing-role-remove-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        context.UserRoles.Add(new UserRole(user.Id, premiumRole.Id));
        await context.SaveChangesAsync();

        var service = new UserRoleMembershipService(context);

        await service.RemoveRoleAsync(user.Id, RoleNames.Premium);
        await service.RemoveRoleAsync(user.Id, RoleNames.Premium);

        int roleCount = await context.UserRoles.CountAsync(userRole =>
            userRole.UserId == user.Id &&
            userRole.RoleId == premiumRole.Id);
        Assert.Equal(0, roleCount);
    }
}
