using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SystemContentOwnerRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SystemOwner_PersistsWithoutAccessRolesOrHumanAdministrationVisibility() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.CreateSystemContentOwner();
        var human = User.Create($"human-{Guid.NewGuid():N}@example.com", "hash");
        context.AddRange(owner, human);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var membership = new UserRoleMembershipService(context.Database, context.Users, context.Roles, context.UserRoles);

        await Assert.ThrowsAsync<InvalidOperationException>(() => membership.EnsureRoleAsync(
            SystemContentOwner.Id, RoleNames.Premium));

        Assert.False(await new UserAccessTokenSecurityReader(context.Users).IsCurrentAsync(owner.Id.Value, owner.SecurityVersion));
        Assert.Empty(await context.UserRoles.Where(role => role.UserId == owner.Id).ToListAsync());
        var lookup = new UserRepository(context.Users, context.Set<UserRoleAuditEvent>());
        Assert.Null(await lookup.GetByIdAsync(owner.Id));
        User? storedOwner = await lookup.GetByIdIncludingDeletedAsync(owner.Id);
        Assert.NotNull(storedOwner);
        Assert.Multiple(
            () => Assert.False(storedOwner.IsActive),
            () => Assert.Null(storedOwner.Email),
            () => Assert.Empty(storedOwner.Password),
            () => Assert.Null(storedOwner.DeletedAt));
        var administration = new UserAdministrationReadRepository(context.Users, context.UserRoles);
        (IReadOnlyList<User> users, int total) = await administration.GetPagedAsync(
            search: null, page: 1, limit: 100, UserAccountStatusFilter.All);
        Assert.Multiple(
            () => Assert.Equal(1, total),
            () => Assert.Equal(human.Id, Assert.Single(users).Id));
        Assert.Null(await administration.GetByIdIncludingDeletedReadModelAsync(owner.Id));
        (int totalUsers, int activeUsers, int premiumUsers, int deletedUsers, IReadOnlyList<User> recentUsers) =
            await administration.GetAdminDashboardSummaryAsync(recentLimit: 10);
        Assert.Multiple(
            () => Assert.Equal(1, totalUsers),
            () => Assert.Equal(1, activeUsers),
            () => Assert.Equal(0, premiumUsers),
            () => Assert.Equal(0, deletedUsers),
            () => Assert.Equal(human.Id, Assert.Single(recentUsers).Id));
    }
}
