using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BrowserSessionActivityPersistenceTests(PostgresDatabaseFixture fixture) {
    [RequiresDockerFact]
    public async Task Activity_IsThrottledAcrossConcurrentRequests_AndCannotOverwriteRevocationProofOrExpiry() {
        await using FoodDiaryDbContext context = await fixture.CreateDbContextAsync();
        DateTime now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"activity-atomic-{Guid.NewGuid():N}@example.com", "hash");
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "proof-hash", rememberMe: false,
            "password", ipAddress: null, userAgent: null, now.AddMinutes(-6), now.AddDays(30));
        session.ConvertToBrowserSession("proof-hash", 7, now.AddMinutes(-6));
        context.Users.Add(user);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        RefreshTokenSessionRepository repository = Repository(context, now);
        BrowserSessionProof proof = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        Assert.Empty(context.ChangeTracker.Entries());
        string connection = context.Database.GetConnectionString()!;
        bool[] updates = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ => {
            await using FoodDiaryDbContext concurrent = fixture.CreateDbContext(connection);
            return await Repository(concurrent, now).TryRecordBrowserActivityAsync(proof, now, now.AddMinutes(-5));
        }));
        Assert.Equal(1, updates.Count(updated => updated));
        DateTime later = now.AddMinutes(5);
        Assert.False(await repository.TryRecordBrowserActivityAsync(proof with { UserId = UserId.New() }, later, now));
        Assert.False(await repository.TryRecordBrowserActivityAsync(proof with { CredentialHash = "wrong-proof" }, later, now));
        Assert.False(await repository.TryRecordBrowserActivityAsync(proof with { SecurityVersion = 8 }, later, now));
        await repository.RevokeByIdAsync(session.Id, user.Id, later);
        Assert.False(await repository.TryRecordBrowserActivityAsync(proof, later, now));
        BrowserSessionProof current = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        Assert.Equal(later, current.RevokedAtUtc);
        Assert.Equal(now, current.LastActiveAtUtc);
        Assert.Equal(proof.ExpiresAtUtc, current.ExpiresAtUtc);
        Assert.Equal(proof.CredentialHash, current.CredentialHash);
        Assert.Equal(7, current.SecurityVersion);
        Assert.Empty(context.ChangeTracker.Entries());
        var expiring = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "expiring-proof", rememberMe: false,
            "password", ipAddress: null, userAgent: null, now.AddMinutes(-6), now.AddMinutes(1));
        expiring.ConvertToBrowserSession("expiring-proof", 7, now.AddMinutes(-6));
        context.UserRefreshTokenSessions.Add(expiring);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        BrowserSessionProof expiredProof = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(expiring.Id));
        Assert.False(await repository.TryRecordBrowserActivityAsync(expiredProof, later, now));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.TryRecordBrowserActivityAsync(
            expiredProof, now, now.AddMinutes(-5), new CancellationToken(canceled: true)));
    }

    private static RefreshTokenSessionRepository Repository(FoodDiaryDbContext context, DateTime now) => new(
        context.UserRefreshTokenSessions, context.Database, new FixedClock(now),
        Options.Create(new JwtOptions { RefreshTokenExpirationDays = 30, RememberMeRefreshTokenExpirationDays = 90 }));

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
