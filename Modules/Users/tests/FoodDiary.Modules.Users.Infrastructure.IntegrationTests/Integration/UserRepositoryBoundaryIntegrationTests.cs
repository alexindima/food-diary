using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserRepositoryBoundaryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Lookups_PreserveActiveAndInclusiveFiltersAndCompositeGoogleIdentity() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("active-boundary@example.com", "hash");
        var inactive = User.Create("inactive-boundary@example.com", "hash");
        var deleted = User.Create("deleted-boundary@example.com", "hash");
        active.LinkTelegram(1001);
        inactive.LinkTelegram(1002);
        deleted.LinkTelegram(1003);
        active.LinkGoogleIdentity("issuer-a", "shared-subject");
        inactive.LinkGoogleIdentity("issuer-b", "shared-subject");
        deleted.LinkGoogleIdentity("issuer-a", "deleted-subject");
        inactive.Deactivate();
        deleted.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(active, inactive, deleted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        Assert.NotNull(active.Email);
        Assert.Equal(active.Id, (await repository.GetByEmailAsync(active.Email))?.Id);
        Assert.Equal(active.Id, (await repository.GetByIdAsync(active.Id))?.Id);
        Assert.Equal(active.Id, (await repository.GetByTelegramUserIdAsync(1001))?.Id);
        foreach (User excluded in new[] { inactive, deleted }) {
            Assert.NotNull(excluded.Email);
            Assert.Null(await repository.GetByEmailAsync(excluded.Email));
            Assert.Null(await repository.GetByIdAsync(excluded.Id));
            Assert.Null(await repository.GetByTelegramUserIdAsync(excluded.TelegramUserId!.Value));
            Assert.Equal(excluded.Id, (await repository.GetByEmailIncludingDeletedAsync(excluded.Email))?.Id);
            Assert.Equal(excluded.Id, (await repository.GetByIdIncludingDeletedAsync(excluded.Id))?.Id);
            Assert.Equal(excluded.Id, (await repository.GetByTelegramUserIdIncludingDeletedAsync(excluded.TelegramUserId.Value))?.Id);
        }

        Assert.Equal(active.Id, (await repository.GetByGoogleIdentityIncludingDeletedAsync("issuer-a", "shared-subject"))?.Id);
        Assert.Equal(inactive.Id, (await repository.GetByGoogleIdentityIncludingDeletedAsync("issuer-b", "shared-subject"))?.Id);
        Assert.Equal(deleted.Id, (await repository.GetByGoogleIdentityIncludingDeletedAsync("issuer-a", "deleted-subject"))?.Id);
        Assert.Null(await repository.GetByGoogleIdentityIncludingDeletedAsync("issuer-b", "deleted-subject"));
        Assert.Null(await repository.GetByEmailIncludingDeletedAsync("absent@example.com"));
        Assert.Null(await repository.GetByIdIncludingDeletedAsync(UserId.New()));
        Assert.Null(await repository.GetByTelegramUserIdIncludingDeletedAsync(9999));
    }

    [RequiresDockerFact]
    public async Task Lookups_ReuseTrackedAggregateAndPreservePendingChangesAndGoalLoading() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("tracked-boundary@example.com", "hash");
        user.UpdatePersonalInfo(firstName: "Persisted");
        user.StartWeightGoal(70, 80, DateTime.UtcNow);
        user.StartWaistGoal(80, 90, DateTime.UtcNow);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new UserRepository(context);

        Assert.NotNull(user.Email);
        User? byEmail = await repository.GetByEmailAsync(user.Email);
        Assert.NotNull(byEmail);
        Assert.Empty(byEmail.WeightGoals);
        Assert.Empty(byEmail.WaistGoals);
        byEmail.UpdatePersonalInfo(firstName: "Pending");
        User? byId = await repository.GetByIdAsync(user.Id);
        Assert.Same(byEmail, byId);
        Assert.Multiple(
            () => Assert.Equal("Pending", byEmail.FirstName),
            () => Assert.Single(byEmail.WeightGoals),
            () => Assert.Single(byEmail.WaistGoals),
            () => Assert.Single(context.ChangeTracker.Entries<User>()));
        Assert.Equal("Persisted", await context.Users.AsNoTracking().Where(item => item.Id == user.Id).Select(item => item.FirstName).SingleAsync());
        await repository.UpdateAsync(byEmail);
        Assert.Equal("Persisted", await context.Users.AsNoTracking().Where(item => item.Id == user.Id).Select(item => item.FirstName).SingleAsync());
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.Equal("Pending", (await repository.GetByEmailAsync(user.Email))?.FirstName);
    }

    [RequiresDockerFact]
    public async Task AddAndDetachedUpdate_StageChangesWithoutBypassingShadowConcurrency() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new UserRepository(context);
        var user = User.Create("staged-boundary@example.com", "hash");
        Assert.Same(user, await repository.AddAsync(user));
        Assert.Equal(EntityState.Added, context.Entry(user).State);
        Assert.False(await context.Users.AsNoTracking().AnyAsync(item => item.Id == user.Id));
        await context.SaveChangesAsync();
        uint persistedVersion = context.Entry(user).Property<uint>("xmin").CurrentValue;
        Assert.NotEqual(0u, persistedVersion);
        context.ChangeTracker.Clear();
        user.UpdatePersonalInfo(firstName: "Detached update");

        await repository.UpdateAsync(user, Array.Empty<UserRoleAuditEvent>());

        Assert.Equal(EntityState.Modified, context.Entry(user).State);
        Assert.Equal(0u, context.Entry(user).Property<uint>("xmin").OriginalValue);
        Assert.Null(await context.Users.AsNoTracking().Where(item => item.Id == user.Id).Select(item => item.FirstName).SingleAsync());
        Assert.Empty(await context.UserRoleAuditEvents.ToListAsync());
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();
        User? persisted = await repository.GetByIdAsync(user.Id);
        Assert.NotNull(persisted);
        Assert.Null(persisted.FirstName);
        Assert.Equal(persistedVersion, context.Entry(persisted).Property<uint>("xmin").CurrentValue);
    }

    [RequiresDockerFact]
    public async Task UpdateWithRoleAudit_PreservesCallerTransactionCommitAndRollback() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("audit-boundary@example.com", "hash");
        user.UpdatePersonalInfo(firstName: "Original");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        Role premium = await context.Roles.SingleAsync(role => role.Name == RoleNames.Premium);
        var repository = new UserRepository(context);
        var rolledBack = UserRoleAuditEvent.Create(user.Id, premium, UserRoleAuditAction.Added, actorUserId: null, source: "users-test", occurredAtUtc: DateTime.UtcNow);
        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
            user.UpdatePersonalInfo(firstName: "Rolled back");
            await repository.UpdateAsync(user, new[] { rolledBack });
            Assert.Equal(EntityState.Added, context.Entry(rolledBack).State);
            Assert.False(await context.UserRoleAuditEvents.AsNoTracking().AnyAsync(item => item.Id == rolledBack.Id));
            await context.SaveChangesAsync();
            Assert.True(await context.UserRoleAuditEvents.AsNoTracking().AnyAsync(item => item.Id == rolledBack.Id));
            await transaction.RollbackAsync();
        }

        context.ChangeTracker.Clear();
        User? persisted = await repository.GetByIdAsync(user.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Original", persisted.FirstName);
        Assert.False(await context.UserRoleAuditEvents.AsNoTracking().AnyAsync(item => item.Id == rolledBack.Id));
        var committed = UserRoleAuditEvent.Create(user.Id, premium, UserRoleAuditAction.Added, actorUserId: null, source: "users-test", occurredAtUtc: DateTime.UtcNow);
        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
            persisted.UpdatePersonalInfo(firstName: "Committed");
            await repository.UpdateAsync(persisted, new[] { committed });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        context.ChangeTracker.Clear();
        Assert.Equal("Committed", (await repository.GetByIdAsync(user.Id))?.FirstName);
        UserRoleAuditEvent audit = await context.UserRoleAuditEvents.SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(committed.Id, audit.Id),
            () => Assert.Equal(user.Id, audit.UserId),
            () => Assert.Equal(premium.Id, audit.RoleId),
            () => Assert.Equal(UserRoleAuditAction.Added, audit.Action),
            () => Assert.Null(audit.ActorUserId),
            () => Assert.Equal("users-test", audit.Source));
    }

    [RequiresDockerFact]
    public async Task LookupMethods_PropagateCancellationWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new UserRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        CancellationToken token = cancellation.Token;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByEmailAsync("cancel@example.com", token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByEmailIncludingDeletedAsync("cancel@example.com", token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByIdAsync(UserId.New(), token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByIdIncludingDeletedAsync(UserId.New(), token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByTelegramUserIdAsync(9999, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByTelegramUserIdIncludingDeletedAsync(9999, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByGoogleIdentityIncludingDeletedAsync("issuer", "subject", token));
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
