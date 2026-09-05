using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ActiveSessionProjectionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ActiveSessionProjection_IsUserScopedOrderedAndDoesNotTrackTokenEntities() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"session-projection-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"session-other-{Guid.NewGuid():N}@example.com", "hash");
        DateTime now = DateTime.UtcNow;
        var older = UserRefreshTokenSession.Create(Guid.NewGuid(), user.Id, "old-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: "older-agent", nowUtc: now.AddMinutes(-1));
        var newer = UserRefreshTokenSession.Create(Guid.NewGuid(), user.Id, "new-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: "newer-agent", nowUtc: now);
        var revoked = UserRefreshTokenSession.Create(Guid.NewGuid(), user.Id, "revoked-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: null, nowUtc: now);
        revoked.Revoke(now);
        var foreign = UserRefreshTokenSession.Create(Guid.NewGuid(), other.Id, "foreign-hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: null, nowUtc: now);
        context.Users.AddRange(user, other);
        context.UserRefreshTokenSessions.AddRange(older, newer, revoked, foreign);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new RefreshTokenSessionRepository(context);

        IReadOnlyList<RefreshTokenSessionReadModel> models = await repository.GetActiveReadModelsAsync(user.Id);

        Assert.Equal([newer.Id, older.Id], models.Select(model => model.Id));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal("newer-agent", models[0].UserAgent);
    }
}
