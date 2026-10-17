using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserAccessTokenSecurityReaderIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task IsCurrentAdministratorAsync_RequiresPersistedAdminMembershipWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role adminRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Admin);
        var admin = User.Create($"security-admin-{Guid.NewGuid():N}@example.com", "hash");
        var regular = User.Create($"security-regular-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(admin, regular);
        context.UserRoles.Add(new UserRole(admin.Id, adminRole.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new UserAccessTokenSecurityReader(context.Users);

        Assert.True(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));
        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 1));
        Assert.False(await reader.IsCurrentAdministratorAsync(regular.Id.Value, 0));
        Assert.False(await reader.IsCurrentAdministratorAsync(Guid.NewGuid(), 0));
        Assert.Empty(context.ChangeTracker.Entries());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.IsCurrentAdministratorAsync(admin.Id.Value, 0, cancellation.Token));
    }

    [RequiresDockerFact]
    public async Task IsCurrentAdministratorAsync_RoleRemovalAndRestorationNeverReviveOldVersion() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role adminRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Admin);
        var admin = User.Create($"security-role-revoke-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(admin);
        context.UserRoles.Add(new UserRole(admin.Id, adminRole.Id));
        await context.SaveChangesAsync();
        var reader = new UserAccessTokenSecurityReader(context.Users);
        var roles = new UserRoleMembershipService(context.Database, context.Users, context.Roles, context.UserRoles);
        Assert.True(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));

        await roles.RemoveRoleAsync(admin.Id, RoleNames.Admin);

        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));
        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 1));
        await roles.EnsureRoleAsync(admin.Id, RoleNames.Admin);
        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));
        Assert.True(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 2));
        Assert.Equal(0, admin.SecurityVersion); // The stale tracked aggregate must not authorize a token.
    }

    [RequiresDockerTheory]
    [InlineData("password")]
    [InlineData("deactivate")]
    [InlineData("delete")]
    public async Task IsCurrentAdministratorAsync_UsesSavedAccountAndCredentialRevocation(string change) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        Role adminRole = await context.Roles.SingleAsync(role => role.Name == RoleNames.Admin);
        var admin = User.Create($"security-admin-state-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(admin);
        context.UserRoles.Add(new UserRole(admin.Id, adminRole.Id));
        await context.SaveChangesAsync();
        var reader = new UserAccessTokenSecurityReader(context.Users);
        switch (change) {
            case "password":
                admin.UpdatePassword("updated-hash");
                break;
            case "deactivate":
                admin.Deactivate();
                break;
            case "delete":
                admin.MarkDeleted(DateTime.UtcNow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }

        Assert.True(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));
        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, admin.SecurityVersion));
        await context.SaveChangesAsync();

        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));
        Assert.Equal(string.Equals(change, "password", StringComparison.Ordinal), await reader.IsCurrentAdministratorAsync(admin.Id.Value, admin.SecurityVersion));
        if (string.Equals(change, "deactivate", StringComparison.Ordinal)) {
            admin.Activate();
        } else if (string.Equals(change, "delete", StringComparison.Ordinal)) {
            admin.Restore();
        }
        await context.SaveChangesAsync();
        Assert.False(await reader.IsCurrentAdministratorAsync(admin.Id.Value, 0));
        Assert.True(await reader.IsCurrentAdministratorAsync(admin.Id.Value, admin.SecurityVersion));
    }

    [RequiresDockerFact]
    public async Task IsCurrentAsync_RejectsOldVersionAndInactiveUser() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"security-version-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var repository = new UserAccessTokenSecurityReader(context.Users);

        Assert.True(await repository.IsCurrentAsync(user.Id.Value, securityVersion: 0));

        user.UpdatePassword("next-hash");
        await context.SaveChangesAsync();

        Assert.False(await repository.IsCurrentAsync(user.Id.Value, securityVersion: 0));
        Assert.True(await repository.IsCurrentAsync(user.Id.Value, securityVersion: 1));

        user.Deactivate();
        await context.SaveChangesAsync();

        Assert.False(await repository.IsCurrentAsync(user.Id.Value, securityVersion: 2));
    }

    [RequiresDockerFact]
    public async Task IsCurrentAsync_RejectsMissingInactiveDeletedAndWrongVersionWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var active = User.Create("security-active@example.com", "hash");
        var inactive = User.Create("security-inactive@example.com", "hash");
        var deleted = User.Create("security-deleted@example.com", "hash");
        inactive.Deactivate();
        deleted.MarkDeleted(DateTime.UtcNow);
        context.Users.AddRange(active, inactive, deleted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new UserAccessTokenSecurityReader(context.Users);

        Assert.True(await reader.IsCurrentAsync(active.Id.Value, active.SecurityVersion));
        Assert.False(await reader.IsCurrentAsync(Guid.NewGuid(), 0));
        Assert.False(await reader.IsCurrentAsync(active.Id.Value, active.SecurityVersion + 1));
        Assert.False(await reader.IsCurrentAsync(inactive.Id.Value, inactive.SecurityVersion));
        Assert.False(await reader.IsCurrentAsync(deleted.Id.Value, deleted.SecurityVersion));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task ScopedReader_UsesPersistedStateWhileRepositoryPreservesTrackedAggregate() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddUsersPersistence();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.Replace(ServiceDescriptor.Scoped(_ => databaseFixture.CreateDbContext(connectionString)));
        services.AddScoped<SharedPersistenceDbContext>(provider => provider.GetRequiredService<FoodDiaryDbContext>());
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        await context.Database.MigrateAsync();
        IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserAccessTokenSecurityReader reader = scope.ServiceProvider.GetRequiredService<IUserAccessTokenSecurityReader>();
        var user = User.Create("security-tracked@example.com", "hash");
        await repository.AddAsync(user);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        Assert.Same(user, await repository.GetByIdAsync(user.Id));
        user.UpdatePassword("next-hash");
        await repository.UpdateAsync(user);
        Assert.True(await reader.IsCurrentAsync(user.Id.Value, 0));
        Assert.False(await reader.IsCurrentAsync(user.Id.Value, 1));
        Assert.Same(user, Assert.Single(scope.ServiceProvider.GetRequiredService<UsersDbContext>().ChangeTracker.Entries<User>()).Entity);

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.False(await reader.IsCurrentAsync(user.Id.Value, 0));
        Assert.True(await reader.IsCurrentAsync(user.Id.Value, 1));
        scope.ServiceProvider.GetRequiredService<UsersDbContext>().ChangeTracker.Clear();
        Assert.True(await reader.IsCurrentAsync(user.Id.Value, 1));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<UsersDbContext>().ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task IsCurrentAsync_PropagatesCancellationWithoutChangingPersistedState() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("security-cancellation@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new UserAccessTokenSecurityReader(context.Users);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.IsCurrentAsync(user.Id.Value, user.SecurityVersion, cancellation.Token));

        Assert.True(await reader.IsCurrentAsync(user.Id.Value, user.SecurityVersion));
        Assert.Empty(context.ChangeTracker.Entries());
    }
    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
