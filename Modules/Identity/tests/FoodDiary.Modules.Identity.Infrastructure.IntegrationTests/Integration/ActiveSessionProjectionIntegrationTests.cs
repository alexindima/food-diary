using Microsoft.Extensions.Options;
using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ActiveSessionProjectionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ActiveSessionProjection_ExcludesExpiredExplicitAndLegacySessionsBeforePagination() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"expiry-owner-{Guid.NewGuid():N}@example.com", "hash");
        var foreign = User.Create($"expiry-foreign-{Guid.NewGuid():N}@example.com", "hash");
        var now = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        UserRefreshTokenSession Create(int ageDays, bool rememberMe = false, DateTime? expires = null, bool otherOwner = false) =>
            UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), otherOwner ? foreign.Id : owner.Id,
                "hash", rememberMe, "password", ipAddress: null, "same-agent", now.AddDays(-ageDays), expires);
        UserRefreshTokenSession regular = Create(1);
        UserRefreshTokenSession remembered = Create(31, rememberMe: true);
        UserRefreshTokenSession explicitExpiry = Create(100, expires: now.AddMinutes(1));
        UserRefreshTokenSession revoked = Create(1, expires: now.AddDays(1));
        revoked.Revoke(now);
        context.Users.AddRange(owner, foreign);
        context.UserRefreshTokenSessions.AddRange(regular, remembered, explicitExpiry, revoked,
            Create(1, expires: now), Create(1, expires: now.AddSeconds(-1)),
            Create(30), Create(90, rememberMe: true), Create(1, otherOwner: true));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database,
            new FixedTimeProvider(now), Options.Create(new JwtOptions { RefreshTokenExpirationDays = 30, RememberMeRefreshTokenExpirationDays = 90 }));

        IReadOnlyList<RefreshTokenSessionReadModel> all = await repository.GetActiveReadModelsAsync(owner.Id);
        var pages = new List<RefreshTokenSessionReadModel>();
        for (int page = 1; page <= 4; page++) {
            pages.AddRange(await repository.GetActivePageReadModelsAsync(owner.Id, page, 1));
        }

        Assert.Multiple(
            () => Assert.Equal(new[] { regular.Id, remembered.Id, explicitExpiry.Id }, all.Select(session => session.Id)),
            () => Assert.Equal(all.Select(session => session.Id), pages.Select(session => session.Id)),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        Assert.Equal(all.Select(session => session.Id), (await repository.GetActiveByUserIdAsync(owner.Id)).Select(session => session.Id));
        foreach (UserRefreshTokenSession session in await context.UserRefreshTokenSessions.Where(session => session.UserId == owner.Id).ToListAsync()) {
            bool expected = all.Any(active => active.Id == session.Id);
            Assert.True(await repository.IsActiveAsync(owner.Id, session.Id) == expected,
                "Session access checks must enforce explicit and legacy expiration, including the exact expiry boundary.");
        }
    }

    [RequiresDockerFact]
    public async Task ActiveSessionProjection_IsUserScopedOrderedAndDoesNotTrackTokenEntities() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"session-projection-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"session-other-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var older = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "old-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: "older-agent", nowUtc: now.AddMinutes(-1));
        var newer = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "new-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: "newer-agent", nowUtc: now);
        var revoked = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "revoked-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: null, nowUtc: now);
        revoked.Revoke(now);
        var foreign = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), other.Id, "foreign-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: null, nowUtc: now);
        context.Users.AddRange(user, other);
        context.UserRefreshTokenSessions.AddRange(older, newer, revoked, foreign);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database, TimeProvider.System, Options.Create(new JwtOptions { RefreshTokenExpirationDays = 30, RememberMeRefreshTokenExpirationDays = 90 }));

        IReadOnlyList<RefreshTokenSessionReadModel> models = await repository.GetActiveReadModelsAsync(user.Id);

        Assert.Equal([newer.Id, older.Id], models.Select(model => model.Id));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal("newer-agent", models[0].UserAgent);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime nowUtc) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(nowUtc);
    }
}
