using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RefreshSessionIdentityIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TypedSessionKeys_PreserveSchemaRotationAndOwnerCurrentSessionFences() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"session-owner-{Guid.NewGuid():N}@example.com", "hash");
        var foreignOwner = User.Create($"session-foreign-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var current = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), owner.Id, "initial", rememberMe: false,
            authProvider: null, ipAddress: null, userAgent: null, nowUtc: now);
        var other = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), owner.Id, "other", rememberMe: false,
            authProvider: null, ipAddress: null, userAgent: null, nowUtc: now);
        var foreign = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), foreignOwner.Id, "foreign", rememberMe: false,
            authProvider: null, ipAddress: null, userAgent: null, nowUtc: now);
        context.Users.AddRange(owner, foreignOwner);
        context.UserRefreshTokenSessions.AddRange(current, other, foreign);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database);

        Assert.False(context.Database.HasPendingModelChanges());
        Assert.False(await repository.TryRotateAsync(current.Id, foreignOwner.Id, "initial", "wrong-owner", rememberMe: true, now));
        Assert.False(await repository.TryRotateAsync(current.Id, owner.Id, "wrong-hash", "wrong", rememberMe: true, now));
        Assert.True(await repository.TryRotateAsync(current.Id, owner.Id, "initial", "rotated", rememberMe: true, now));
        Assert.False(await repository.TryRotateAsync(current.Id, owner.Id, "initial", "stale", rememberMe: true, now));
        await repository.RevokeOtherByIdAsync(foreign.Id, owner.Id, current.Id, now);
        await repository.RevokeOtherByIdAsync(current.Id, owner.Id, current.Id, now);
        Assert.True(await repository.IsActiveAsync(owner.Id, current.Id));
        Assert.True(await repository.IsActiveAsync(foreignOwner.Id, foreign.Id));
        await repository.RevokeOtherByIdAsync(other.Id, owner.Id, current.Id, now);
        Assert.False(await repository.IsActiveAsync(owner.Id, other.Id));
        await repository.RevokeByIdAsync(current.Id, owner.Id, now);
        await repository.RevokeAllOtherAsync(foreignOwner.Id, current.Id, now);
        Assert.True(await repository.IsActiveAsync(foreignOwner.Id, foreign.Id));
        Assert.Equal(current.Id.Value, (await repository.GetByIdAsync(current.Id))!.Id.Value);
    }
}
