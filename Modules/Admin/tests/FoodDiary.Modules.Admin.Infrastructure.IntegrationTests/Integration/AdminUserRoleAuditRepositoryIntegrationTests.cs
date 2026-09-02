using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AdminUserRoleAuditRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetRecentForUserAsync_WithZeroLimit_ReturnsActorAndAuditFields() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"audit-user-{Guid.NewGuid():N}@example.com", "hash");
        var actor = User.Create($"audit-actor-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(user, actor);
        Role role = await GetPremiumRoleAsync(context);
        var occurredAt = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var stored = UserRoleAuditEvent.Create(user.Id, role, UserRoleAuditAction.Removed, actor.Id, "coverage", occurredAt);
        context.UserRoleAuditEvents.Add(stored);
        await context.SaveChangesAsync();

        var repository = new AdminUserRoleAuditRepository(context);
        IReadOnlyList<AdminUserRoleAuditEventReadModel> events = await repository.GetRecentForUserAsync(user.Id.Value, limit: 0);

        // Preserve the extracted donor assertions and verify the complete projection.
        AdminUserRoleAuditEventReadModel auditEvent = Assert.Single(events);
        Assert.NotNull(auditEvent.ActorEmail);
        Assert.Multiple(
            () => Assert.Equal(stored.Id, auditEvent.Id),
            () => Assert.Equal(user.Id.Value, auditEvent.UserId),
            () => Assert.Equal(role.Name, auditEvent.RoleName),
            () => Assert.Equal("Removed", auditEvent.Action),
            () => Assert.Equal(actor.Id.Value, auditEvent.ActorUserId),
            () => Assert.Equal(actor.Email, auditEvent.ActorEmail),
            () => Assert.Equal("coverage", auditEvent.Source),
            () => Assert.Equal(occurredAt, auditEvent.OccurredAtUtc));
    }

    [RequiresDockerFact]
    public async Task GetRecentForUserAsync_FiltersUserOrdersDescendingAndCapsAtFifty() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"audit-target-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"audit-other-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(user, other);
        Role role = await GetPremiumRoleAsync(context);
        var start = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        UserRoleAuditEvent[] owned = [.. Enumerable.Range(0, 55).Select(index =>
            UserRoleAuditEvent.Create(user.Id, role, UserRoleAuditAction.Added, actorUserId: null, "tests", start.AddMinutes(index)))];
        context.UserRoleAuditEvents.AddRange(owned);
        context.UserRoleAuditEvents.Add(UserRoleAuditEvent.Create(other.Id, role, UserRoleAuditAction.Removed, actorUserId: null, "other", start.AddDays(1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new AdminUserRoleAuditRepository(context);
        IReadOnlyList<AdminUserRoleAuditEventReadModel> events = await repository.GetRecentForUserAsync(user.Id.Value, limit: 999);

        Assert.Multiple(
            () => Assert.Equal(50, events.Count),
            () => Assert.Equal(owned.Reverse().Take(50).Select(item => item.Id), events.Select(item => item.Id)),
            () => Assert.All(events, item => Assert.Equal(user.Id.Value, item.UserId)),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    [RequiresDockerFact]
    public async Task GetRecentForUserAsync_WithNoActor_PreservesRowAndNullActorFields() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"audit-system-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        Role role = await GetPremiumRoleAsync(context);
        var stored = UserRoleAuditEvent.Create(user.Id, role, UserRoleAuditAction.Added, actorUserId: null, "system", DateTime.UtcNow);
        context.UserRoleAuditEvents.Add(stored);
        await context.SaveChangesAsync();

        var repository = new AdminUserRoleAuditRepository(context);
        AdminUserRoleAuditEventReadModel auditEvent = Assert.Single(await repository.GetRecentForUserAsync(user.Id.Value, limit: -1));
        Assert.Multiple(
            () => Assert.Equal(stored.Id, auditEvent.Id),
            () => Assert.Null(auditEvent.ActorUserId),
            () => Assert.Null(auditEvent.ActorEmail));
        Assert.Empty(await repository.GetRecentForUserAsync(Guid.NewGuid(), limit: 50));
    }

    [RequiresDockerFact]
    public async Task GetRecentForUserAsync_WithCancelledToken_PropagatesCancellation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new AdminUserRoleAuditRepository(context);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetRecentForUserAsync(Guid.NewGuid(), limit: 20, cancellation.Token));
    }

    private static async Task<Role> GetPremiumRoleAsync(FoodDiaryDbContext context) {
        Role role = await context.Roles.FirstOrDefaultAsync(item => item.Name == RoleNames.Premium) ?? Role.Create(RoleNames.Premium);
        if (context.Entry(role).State == EntityState.Detached) {
            context.Roles.Add(role);
        }

        return role;
    }
}
