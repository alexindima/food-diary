using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Domain.Primitives;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserAccessTokenSecurityReaderIntegrationTests(PostgresDatabaseFixture databaseFixture) {
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
