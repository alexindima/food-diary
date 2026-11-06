using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
[Collection(PostgresDatabaseCollection.Name)]
public sealed class PremiumAccessEndPersistenceTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime End = new(2026, 12, 6, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task LegacyMembershipService_AllowsUnlimitedAccessButRejectsUnsupportedTimedAccess() {
        var legacy = new LegacyMembershipService();
        IUserRoleMembershipService service = legacy;
        var userId = UserId.New();

        await service.SetPremiumAccessEndAsync(userId, endsAtUtc: null);
        await Assert.ThrowsAsync<NotSupportedException>(() => service.SetPremiumAccessEndAsync(userId, End));

        Assert.Equal(0, legacy.RoleChanges);
    }

    [Fact]
    public async Task InMemoryExpiration_SynchronizesTransactionAndStagesTrackedUserChanges() {
        await using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var user = User.Create("premium-memory@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        using var cancellation = new CancellationTokenSource();
        int synchronizations = 0;
        var service = new UserRoleMembershipService(context.Database, context.Users, context.Roles, context.UserRoles, token => {
            Assert.Equal(cancellation.Token, token);
            Assert.Null(user.PremiumAccessEndsAtUtc);
            synchronizations++;
            return Task.CompletedTask;
        });

        await service.SetPremiumAccessEndAsync(user.Id, End, cancellation.Token);
        await context.SaveChangesAsync(cancellation.Token);
        context.ChangeTracker.Clear();
        Assert.Equal(End, (await context.Users.SingleAsync(cancellation.Token)).PremiumAccessEndsAtUtc);
        // Continue with the newly tracked instance after checking persistence.
        var clear = new UserRoleMembershipService(context.Database, context.Users, context.Roles, context.UserRoles);
        await clear.SetPremiumAccessEndAsync(user.Id, endsAtUtc: null, cancellation.Token);
        await clear.SetPremiumAccessEndAsync(UserId.New(), End, cancellation.Token);
        await context.SaveChangesAsync(cancellation.Token);
        Assert.Multiple(() => {
            Assert.Null(context.Users.Single().PremiumAccessEndsAtUtc);
            Assert.Equal(1, synchronizations);
        });
    }

    [Fact]
    public async Task InvalidExpiration_IsRejectedBeforeTransactionSynchronization() {
        int synchronizations = 0;
        var service = new UserRoleMembershipService(null!, null!, null!, null!, _ => {
            synchronizations++;
            return Task.CompletedTask;
        });

        await Assert.ThrowsAsync<ArgumentException>(() => service.SetPremiumAccessEndAsync(UserId.Empty, End));
        ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SetPremiumAccessEndAsync(UserId.New(), DateTime.SpecifyKind(End, DateTimeKind.Unspecified)));

        Assert.Multiple(() => {
            Assert.Equal("endsAtUtc", error.ParamName);
            Assert.Equal(0, synchronizations);
        });
    }

    [RequiresDockerFact]
    public async Task PostgresExpiration_OnlyShorteningInvalidatesCredentials_AndDeletedUsersAreUntouched() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("premium-postgres@example.com", "hash");
        var deleted = User.Create("premium-deleted@example.com", "hash");
        deleted.MarkDeleted(DateTime.UtcNow);
        long deletedVersion = deleted.SecurityVersion;
        context.Users.AddRange(user, deleted);
        await context.SaveChangesAsync();
        var service = new UserRoleMembershipService(context.Database, context.Users, context.Roles, context.UserRoles);

        await service.SetPremiumAccessEndAsync(user.Id, End);
        await AssertPersistedAsync(End, 1);
        await service.SetPremiumAccessEndAsync(user.Id, End);
        await AssertPersistedAsync(End, 1);
        await service.SetPremiumAccessEndAsync(user.Id, End.AddDays(1));
        await AssertPersistedAsync(End.AddDays(1), 1);
        await service.SetPremiumAccessEndAsync(user.Id, End);
        await AssertPersistedAsync(End, 2);
        await service.SetPremiumAccessEndAsync(user.Id, endsAtUtc: null);
        await AssertPersistedAsync(expiration: null, 2);
        await service.SetPremiumAccessEndAsync(deleted.Id, End);
        await service.SetPremiumAccessEndAsync(UserId.New(), End);
        User persistedDeleted = await context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(item => item.Id == deleted.Id);
        Assert.Multiple(() => {
            Assert.Null(persistedDeleted.PremiumAccessEndsAtUtc);
            Assert.Equal(deletedVersion, persistedDeleted.SecurityVersion);
        });

        async Task AssertPersistedAsync(DateTime? expiration, long version) {
            User persisted = await context.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
            Assert.Multiple(() => {
                Assert.Equal(expiration, persisted.PremiumAccessEndsAtUtc);
                Assert.Equal(version, persisted.SecurityVersion);
            });
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class LegacyMembershipService : IUserRoleMembershipService {
        public int RoleChanges { get; private set; }

        public Task EnsureRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
            RoleChanges++;
            return Task.CompletedTask;
        }

        public Task RemoveRoleAsync(UserId userId, string roleName, CancellationToken cancellationToken = default) {
            RoleChanges++;
            return Task.CompletedTask;
        }
    }
}
