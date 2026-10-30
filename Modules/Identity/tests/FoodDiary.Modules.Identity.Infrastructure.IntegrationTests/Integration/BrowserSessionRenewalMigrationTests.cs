using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BrowserSessionRenewalMigrationTests(PostgresDatabaseFixture fixture) {
    [RequiresDockerFact]
    public async Task Upgrade_EnablesOnlyKnownOrdinaryBrowserSessions_AndPreservesTheirProofAndExpiry() {
        await using FoodDiaryDbContext context = fixture.CreateDbContext(await fixture.CreateIsolatedDatabaseAsync());
        await context.GetService<IMigrator>().MigrateAsync("20261025190554_AddBrowserSessions");
        DateTime now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).UtcDateTime;
        var userId = UserId.New();
        string email = $"renewal-upgrade-{userId.Value:N}@example.com";
        // Seed the historical schema without involving the evolving current EF model.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Users" ("Id", "Email", "Password", "TelegramLinkVerifiedForRecovery", "CreatedOnUtc")
            VALUES ({userId.Value}, {email}, 'hash', FALSE, {now});
            """);
        DateTime expiry = now.AddDays(30);
        foreach ((string provider, long? version) in new (string, long?)[] { ("password", 7), ("admin-sso", 7), ("unknown", 7), ("telegram", null), ("password-reset", 7) }) {
            var id = Guid.NewGuid();
            DateTime? revoked = string.Equals(provider, "password-reset", StringComparison.Ordinal) ? now : null;
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "UserRefreshTokenSessions"
                    ("Id", "UserId", "RefreshTokenHash", "RememberMe", "AuthProvider", "CreatedAtUtc", "LastRotatedAtUtc", "ExpiresAtUtc", "CreatedOnUtc", "BrowserSecurityVersion", "RevokedAtUtc")
                VALUES ({id}, {userId.Value}, 'proof', FALSE, {provider}, {now}, {now}, {expiry}, {now}, {version}, {revoked});
                """);
        }
        await context.Database.MigrateAsync();
        List<UserRefreshTokenSession> sessions = await context.UserRefreshTokenSessions.AsNoTracking().Where(session => session.UserId == userId).ToListAsync();
        Assert.Equal(5, sessions.Count);
        Assert.All(sessions, session => {
            Assert.Equal(string.Equals(session.AuthProvider, "password", StringComparison.Ordinal), session.BrowserSlidingExpirationEnabled);
            Assert.Equal("proof", session.RefreshTokenHash);
            Assert.Equal(expiry, session.ExpiresAtUtc);
            Assert.Equal(now, session.CreatedAtUtc);
            Assert.Equal(string.Equals(session.AuthProvider, "password-reset", StringComparison.Ordinal) ? now : (DateTime?)null, session.RevokedAtUtc);
        });
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
