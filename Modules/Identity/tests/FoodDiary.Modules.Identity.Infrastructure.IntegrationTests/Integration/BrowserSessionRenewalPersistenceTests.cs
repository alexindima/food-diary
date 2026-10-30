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
public sealed class BrowserSessionRenewalPersistenceTests(PostgresDatabaseFixture fixture) {
    [RequiresDockerFact]
    public async Task PersistencePromotion_PreservesProofAndDeadline_AndFencesStaleRenewalAndRevocation() {
        await using FoodDiaryDbContext context = await fixture.CreateDbContextAsync();
        DateTime now = new(2030, 1, 25, 0, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"persistence-atomic-{Guid.NewGuid():N}@example.com", "hash");
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "proof-hash", rememberMe: false,
            "password", ipAddress: null, userAgent: null, now.AddDays(-24), now.AddDays(6));
        session.ConvertToBrowserSession("proof-hash", 7, now.AddDays(-24), allowRenewal: true);
        context.Users.Add(user);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        RefreshTokenSessionRepository repository = Repository(context, now);
        BrowserSessionProof proof = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));

        Assert.False(await repository.TryRememberBrowserSessionAsync(proof with { UserId = UserId.New() }, now));
        Assert.False(await repository.TryRememberBrowserSessionAsync(proof with { CredentialHash = "other" }, now));
        Assert.False(await repository.TryRememberBrowserSessionAsync(proof with { SecurityVersion = 8 }, now));
        Assert.True(await repository.TryRememberBrowserSessionAsync(proof, now));
        BrowserSessionProof current = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        Assert.Multiple(() => {
            Assert.True(current.RememberMe);
            Assert.Equal(proof.CredentialHash, current.CredentialHash);
            Assert.Equal(proof.ExpiresAtUtc, current.ExpiresAtUtc);
            Assert.Equal(proof.LastActiveAtUtc, current.LastActiveAtUtc);
        });
        Assert.False(await repository.TryRenewBrowserSessionAsync(proof, now, now.AddDays(30)));
        Assert.True(await repository.TryRenewBrowserSessionAsync(current, now, now.AddDays(90)));
        Assert.False(await repository.TryRememberBrowserSessionAsync(current, now.AddDays(90)));
        await repository.RevokeByIdAsync(session.Id, user.Id, now);
        Assert.False(await repository.TryRememberBrowserSessionAsync(current, now));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task Renewal_IsAtomic_AndCannotOverwriteRevocationSecurityVersionOrAnotherCredential() {
        await using FoodDiaryDbContext context = await fixture.CreateDbContextAsync();
        DateTime now = new(2030, 1, 25, 0, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"renewal-atomic-{Guid.NewGuid():N}@example.com", "hash");
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "proof-hash", rememberMe: true,
            "password", ipAddress: null, userAgent: null, now.AddDays(-24), now.AddDays(6));
        session.ConvertToBrowserSession("proof-hash", 7, now.AddDays(-24), allowRenewal: true);
        context.Users.Add(user);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        RefreshTokenSessionRepository repository = Repository(context, now);
        BrowserSessionProof proof = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        string connection = context.Database.GetConnectionString()!;
        bool[] results = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ => {
            await using FoodDiaryDbContext concurrent = fixture.CreateDbContext(connection);
            return await Repository(concurrent, now).TryRenewBrowserSessionAsync(proof, now, now.AddDays(90));
        }));
        Assert.Equal(1, results.Count(result => result));
        BrowserSessionProof current = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        Assert.Equal(now.AddDays(90), current.ExpiresAtUtc);
        Assert.Equal(proof.CredentialHash, current.CredentialHash);
        Assert.Equal(proof.LastActiveAtUtc, current.LastActiveAtUtc);
        Assert.False(await repository.TryRenewBrowserSessionAsync(current with { UserId = UserId.New() }, now, now.AddDays(100)));
        Assert.False(await repository.TryRenewBrowserSessionAsync(current with { CredentialHash = "other" }, now, now.AddDays(100)));
        Assert.False(await repository.TryRenewBrowserSessionAsync(current with { SecurityVersion = 8 }, now, now.AddDays(100)));
        Assert.False(await repository.TryRenewBrowserSessionAsync(current, now.AddDays(90), now.AddDays(100)));
        Assert.True(await repository.TryRevalidateBrowserSessionAsync(session.Id, user.Id, "proof-hash", 8, now));
        Assert.False(await repository.TryRenewBrowserSessionAsync(current, now, now.AddDays(100)));
        current = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        await repository.RevokeByIdAsync(session.Id, user.Id, now);
        Assert.False(await repository.TryRenewBrowserSessionAsync(current, now, now.AddDays(100)));
        Assert.Equal(now, (await repository.ReadBrowserSessionProofAsync(session.Id))!.RevokedAtUtc);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task AdministrativeAndUnclassifiedSessions_CannotBeRenewed() {
        await using FoodDiaryDbContext context = await fixture.CreateDbContextAsync();
        DateTime now = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"fixed-session-{Guid.NewGuid():N}@example.com", "hash");
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user.Id, "proof", rememberMe: true,
            "admin-sso", ipAddress: null, userAgent: null, now, now.AddHours(1));
        session.ConvertToBrowserSession("proof", 7, now);
        context.Users.Add(user);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        RefreshTokenSessionRepository repository = Repository(context, now);
        BrowserSessionProof proof = Assert.IsType<BrowserSessionProof>(await repository.ReadBrowserSessionProofAsync(session.Id));
        Assert.False(await repository.TryRenewBrowserSessionAsync(proof, now, now.AddDays(90)));
        Assert.False(await repository.TryRememberBrowserSessionAsync(proof, now));
        Assert.Equal(proof.ExpiresAtUtc, (await repository.ReadBrowserSessionProofAsync(session.Id))!.ExpiresAtUtc);
    }

    private static RefreshTokenSessionRepository Repository(FoodDiaryDbContext context, DateTime now) => new(
        context.UserRefreshTokenSessions, context.Database, new FixedClock(now),
        Options.Create(new JwtOptions { RefreshTokenExpirationDays = 30, RememberMeRefreshTokenExpirationDays = 90 }));

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
