using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SessionRetentionIntegrationTests(PostgresDatabaseFixture fixture) {
    [RequiresDockerFact]
    public async Task Cleanup_DeletesOnlyOldTerminalSessionsInBoundedIdempotentBatches() {
        await using FoodDiaryDbContext context = await fixture.CreateDbContextAsync();
        DateTime now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime cutoff = now.AddDays(-30);
        var owner = User.Create($"retention-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(owner);
        UserRefreshTokenSession Create(DateTime created, DateTime? expires, bool remember = false) =>
            UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), owner.Id, "unchanged-proof", remember,
                "password", ipAddress: null, userAgent: null, created, expires);
        UserRefreshTokenSession oldExpiry = Create(now.AddDays(-200), cutoff);
        UserRefreshTokenSession oldRevoke = Create(now.AddDays(-200), now.AddDays(1));
        oldRevoke.Revoke(cutoff);
        UserRefreshTokenSession oldLegacy = Create(cutoff.AddDays(-30), expires: null);
        UserRefreshTokenSession oldRemembered = Create(cutoff.AddDays(-90), expires: null, remember: true);
        UserRefreshTokenSession active = Create(now.AddDays(-200), now.AddDays(1));
        active.ConvertToBrowserSession("unchanged-proof", 7, now.AddMinutes(-5));
        UserRefreshTokenSession recentExpiry = Create(now.AddDays(-200), cutoff.AddSeconds(1));
        UserRefreshTokenSession recentRevoke = Create(now.AddDays(-200), now.AddDays(1));
        recentRevoke.Revoke(cutoff.AddSeconds(1));
        UserRefreshTokenSession recentLegacy = Create(cutoff.AddDays(-30).AddSeconds(1), expires: null);
        UserRefreshTokenSession recentRemembered = Create(cutoff.AddDays(-90).AddSeconds(1), expires: null, remember: true);
        context.UserRefreshTokenSessions.AddRange(oldExpiry, oldRevoke, oldLegacy, oldRemembered,
            active, recentExpiry, recentRevoke, recentLegacy, recentRemembered);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database,
            new FixedClock(now), Options.Create(new JwtOptions { RefreshTokenExpirationDays = 30, RememberMeRefreshTokenExpirationDays = 90 }));

        Assert.Equal(2, await repository.DeleteInactiveBeforeAsync(cutoff, 2));
        Assert.Equal(2, await repository.DeleteInactiveBeforeAsync(cutoff, 2));
        Assert.Equal(0, await repository.DeleteInactiveBeforeAsync(cutoff, 2));
        RefreshTokenSessionId[] remaining = await context.UserRefreshTokenSessions.Where(session => session.UserId == owner.Id)
            .Select(session => session.Id).ToArrayAsync();
        Assert.Equal(new[] { active.Id, recentExpiry.Id, recentRevoke.Id, recentLegacy.Id, recentRemembered.Id }.OrderBy(id => id.Value),
            remaining.OrderBy(id => id.Value));
        UserRefreshTokenSession survivor = await context.UserRefreshTokenSessions.SingleAsync(session => session.Id == active.Id);
        Assert.Equal("unchanged-proof", survivor.RefreshTokenHash);
        Assert.Equal(7, survivor.BrowserSecurityVersion);
        Assert.Equal(now.AddDays(1), survivor.ExpiresAtUtc);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.DeleteInactiveBeforeAsync(now.AddSeconds(1), 2));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.DeleteInactiveBeforeAsync(cutoff, 0));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.DeleteInactiveBeforeAsync(cutoff, 2, new CancellationToken(canceled: true)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
