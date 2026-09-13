using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.ReadModel.Composition;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedUsersContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OwnerSavesBeforeSharedAndDependentRegardlessOfResolutionOrderAsync(bool resolveIdentityFirst) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        if (resolveIdentityFirst) { _ = provider.GetRequiredService<IdentityDbContext>(); }
        var user = User.Create("users-context@example.com", "hash");
        await provider.GetRequiredService<IUserWriteRepository>().AddAsync(user);
        await provider.GetRequiredService<IRefreshTokenSessionWriteRepository>().AddAsync(CreateSession(user.Id));
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        UsersDbContext owned = provider.GetRequiredService<UsersDbContext>();
        shared.WeightEntries.Add(WeightEntry.Create(user.Id, DateTime.UtcNow, 70));
        Assert.Equal(6, owned.Model.GetEntityTypes().Count());
        Assert.Same(shared.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(shared.ChangeTracker.Entries<User>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.SaveChangesAsync());
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Single(await database.Users.ToListAsync());
        Assert.Single(await database.WeightEntries.ToListAsync());
        Assert.Single(await database.UserRefreshTokenSessions.ToListAsync());
        Assert.False(database.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task DependentFailureRollsBackOwnerAndSharedWritesAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        var user = User.Create("users-failed-save@example.com", "hash");
        await provider.GetRequiredService<IUserWriteRepository>().AddAsync(user);
        provider.GetRequiredService<FoodDiaryDbContext>().WeightEntries.Add(WeightEntry.Create(user.Id, DateTime.UtcNow, 70));
        await provider.GetRequiredService<IRefreshTokenSessionWriteRepository>().AddAsync(CreateSession(UserId.New()));
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        Assert.Empty(await database.Users.ToListAsync());
        Assert.Empty(await database.WeightEntries.ToListAsync());
        Assert.Empty(await database.UserRefreshTokenSessions.ToListAsync());
        Assert.True(provider.GetRequiredService<IUnitOfWork>().HasPendingChanges);
    }

    [RequiresDockerFact]
    public async Task RoleChangesAfterIntermediateSaveRejoinOuterTransactionAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        var user = User.Create("users-role-rollback@example.com", "hash");
        await using (IDbContextTransaction transaction = await shared.Database.BeginTransactionAsync()) {
            await provider.GetRequiredService<IUserWriteRepository>().AddAsync(user);
            await provider.GetRequiredService<IUserRoleCatalogService>().EnsureRolesByNamesAsync(["context-test-role"]);
            await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            await provider.GetRequiredService<IUserRoleMembershipService>().EnsureRoleAsync(user.Id, "context-test-role");
            Assert.Same(transaction.GetDbTransaction(), provider.GetRequiredService<UsersDbContext>().Database.CurrentTransaction!.GetDbTransaction());
            Assert.True(await provider.GetRequiredService<IUserAccessTokenSecurityReader>().IsCurrentAsync(user.Id.Value, user.SecurityVersion + 1));
            Assert.False(await provider.GetRequiredService<IUserAccessTokenSecurityReader>().IsCurrentAsync(user.Id.Value, user.SecurityVersion));
            await transaction.RollbackAsync();
        }
        Assert.Empty(await database.Users.ToListAsync());
        Assert.False(await database.Roles.AnyAsync(role => role.Name == "context-test-role"));
        Assert.Empty(await database.UserRoles.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task TelegramConflictIsTranslatedByOwnerSaveInterceptorAsync() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!);
        var first = User.Create("users-first@example.com", "hash");
        first.LinkTelegram(777);
        await provider.GetRequiredService<IUserWriteRepository>().AddAsync(first);
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        var second = User.Create("users-second@example.com", "hash");
        second.LinkTelegram(777);
        await provider.GetRequiredService<IUserWriteRepository>().AddAsync(second);
        DbUpdateConcurrencyException error = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        Assert.Null(error.InnerException);
        Assert.Single(await database.Users.ToListAsync());
    }

    private static UserRefreshTokenSession CreateSession(UserId userId) =>
        UserRefreshTokenSession.Create(Guid.NewGuid(), userId, "synthetic", rememberMe: true, authProvider: "password", ipAddress: null, userAgent: null, nowUtc: DateTime.UtcNow);

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Database:EnableRetries"] = "false",
        }).Build());
        services.AddUsersPersistence();
        services.AddIdentityPersistence();
        services.AddReadModelComposition();
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
