using FoodDiary.Authentication.Contracts.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Infrastructure.Persistence.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests;

public sealed partial class RefreshTokenSessionFallbackTests {
    [Fact]
    public async Task AdminSsoFallback_RequiresTheActiveOwnerSessionAfterTransactionSynchronization() {
        await using FoodDiaryDbContext context = CreateContext();
        var owner = UserId.New();
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), owner, "synthetic-hash", rememberMe: false,
            authProvider: "password", ipAddress: null, userAgent: null, Now.AddDays(-1), Now.AddDays(1));
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        int synchronizations = 0;
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database, new RetentionClock(),
            Options.Create(new JwtOptions()), _ => { synchronizations++; return Task.CompletedTask; });

        Assert.True(await repository.LockActiveForAdminSsoAsync(owner, session.Id));
        Assert.False(await repository.LockActiveForAdminSsoAsync(UserId.New(), session.Id));
        session.Revoke(Now);
        await context.SaveChangesAsync();
        Assert.False(await repository.LockActiveForAdminSsoAsync(owner, session.Id));
        Assert.Equal(3, synchronizations);
    }

    [Fact]
    public async Task MissingSecurityVersion_CannotRecordActivityOrRememberABrowserSession() {
        await using FoodDiaryDbContext context = CreateContext();
        int synchronizations = 0;
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database, new RetentionClock(), Options.Create(new JwtOptions()),
            _ => { synchronizations++; return Task.CompletedTask; });
        var proof = new BrowserSessionProof(RefreshTokenSessionId.New(), UserId.New(), "synthetic-hash", SecurityVersion: null,
            Now.AddDays(1), RevokedAtUtc: null, Now);

        bool activity = await repository.TryRecordBrowserActivityAsync(proof, Now, Now.AddMinutes(-5));
        bool remembered = await repository.TryRememberBrowserSessionAsync(proof, Now);

        Assert.Multiple(() => Assert.False(activity), () => Assert.False(remembered));

        Assert.Equal(0, synchronizations);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task BrowserRevalidationFallback_RequiresMatchingProofAndKeepsActivityMonotonic() {
        await using FoodDiaryDbContext context = CreateContext();
        var owner = UserId.New();
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), owner, "synthetic-hash", rememberMe: false,
            authProvider: "password", ipAddress: null, userAgent: null, Now.AddDays(-1), Now.AddDays(1));
        session.ConvertToBrowserSession("synthetic-hash", 7, Now);
        session.RefreshBrowserSession(7, Now);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database, new RetentionClock(), Options.Create(new JwtOptions()));

        Assert.False(await repository.TryRevalidateBrowserSessionAsync(session.Id, owner, "wrong", 9, Now));
        Assert.True(await repository.TryRevalidateBrowserSessionAsync(session.Id, owner, "synthetic-hash", 9, Now.AddMinutes(-1)));

        Assert.Multiple(
            () => Assert.Equal(9, session.BrowserSecurityVersion),
            () => Assert.Equal(Now, session.LastRotatedAtUtc),
            () => Assert.Equal(Now, session.ModifiedOnUtc));
    }

    [Fact]
    public async Task Retention_RejectsFutureOrUnspecifiedCutoffsBeforeDatabaseWork() {
        await using FoodDiaryDbContext context = CreateContext();
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database, new RetentionClock(), Options.Create(new JwtOptions()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.DeleteInactiveBeforeAsync(Now.AddDays(1), 100));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.DeleteInactiveBeforeAsync(DateTime.SpecifyKind(Now, DateTimeKind.Unspecified), 100));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Retention_PropagatesTransactionSynchronizationFailureBeforeDeletingSessions() {
        await using FoodDiaryDbContext context = CreateContext();
        var failure = new InvalidOperationException("Synthetic transaction synchronization failure");
        var repository = new RefreshTokenSessionRepository(context.UserRefreshTokenSessions, context.Database, new RetentionClock(), Options.Create(new JwtOptions()),
            _ => Task.FromException(failure));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DeleteInactiveBeforeAsync(Now.AddDays(-1), 100)));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [ExcludeFromCodeCoverage]
    private sealed class RetentionClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }
}
