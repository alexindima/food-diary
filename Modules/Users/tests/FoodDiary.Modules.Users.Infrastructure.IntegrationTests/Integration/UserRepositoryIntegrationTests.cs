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
    public async Task SurfaceStyle_RoundTripsAndSurvivesUnrelatedProfileUpdate() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"surface-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        User loaded = await context.Users.SingleAsync(item => item.Id == user.Id);
        Assert.Equal("normal", loaded.SurfaceStyle);
        loaded.UpdatePreferences(new FoodDiary.Domain.ValueObjects.UserPreferenceUpdate(SurfaceStyle: "matte"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        loaded = await context.Users.SingleAsync(item => item.Id == user.Id);
        Assert.Equal("matte", loaded.SurfaceStyle);
        loaded.UpdatePreferences(new FoodDiary.Domain.ValueObjects.UserPreferenceUpdate(Theme: "dark"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        loaded = await context.Users.SingleAsync(item => item.Id == user.Id);
        Assert.Equal("matte", loaded.SurfaceStyle);
        Assert.Equal("dark", loaded.Theme);
    }

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

        Assert.NotNull(user.Email);
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
