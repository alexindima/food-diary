using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class RefreshTokenSessionFallbackTests {
    private static readonly DateTime Now = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokeOtherSessions_RequiresActiveCurrentSessionAndPreservesOwnerScope(bool all) {
        await using FoodDiaryDbContext context = CreateContext();
        var owner = UserId.New();
        UserRefreshTokenSession current = CreateSession(owner);
        UserRefreshTokenSession target = CreateSession(owner);
        UserRefreshTokenSession foreign = CreateSession(UserId.New());
        context.UserRefreshTokenSessions.AddRange(current, target, foreign);
        await context.SaveChangesAsync();
        var repository = new RefreshTokenSessionRepository(context);

        if (all) {
            await repository.RevokeAllOtherAsync(owner, foreign.Id, Now);
        } else {
            await repository.RevokeOtherByIdAsync(target.Id, owner, foreign.Id, Now);
            await repository.RevokeOtherByIdAsync(current.Id, owner, current.Id, Now);
            await repository.RevokeOtherByIdAsync(foreign.Id, owner, current.Id, Now);
        }
        Assert.All(new[] { current, target, foreign }, session => Assert.True(session.IsActive));

        if (all) {
            await repository.RevokeAllOtherAsync(owner, current.Id, Now);
        } else {
            await repository.RevokeOtherByIdAsync(target.Id, owner, current.Id, Now);
        }
        Assert.Multiple(
            () => Assert.True(current.IsActive),
            () => Assert.True(foreign.IsActive),
            () => Assert.Equal(Now, target.RevokedAtUtc));
    }

    [Fact]
    public async Task RevokeById_IsOwnerScopedAndIdempotentForMissingSession() {
        await using FoodDiaryDbContext context = CreateContext();
        var owner = UserId.New();
        UserRefreshTokenSession session = CreateSession(owner);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        var repository = new RefreshTokenSessionRepository(context);

        await repository.RevokeByIdAsync(Guid.NewGuid(), owner, Now);
        await repository.RevokeByIdAsync(session.Id, UserId.New(), Now);
        Assert.True(session.IsActive);
        await repository.RevokeByIdAsync(session.Id, owner, Now);
        Assert.Equal(Now, session.RevokedAtUtc);
    }

    [Fact]
    public async Task TryRotate_RequiresOwnerActiveSessionAndMatchingHash() {
        await using FoodDiaryDbContext context = CreateContext();
        var owner = UserId.New();
        UserRefreshTokenSession session = CreateSession(owner);
        context.UserRefreshTokenSessions.Add(session);
        await context.SaveChangesAsync();
        var repository = new RefreshTokenSessionRepository(context);

        Assert.False(await repository.TryRotateAsync(Guid.NewGuid(), owner, "hash", "new", rememberMe: true, Now));
        Assert.False(await repository.TryRotateAsync(session.Id, UserId.New(), "hash", "new", rememberMe: true, Now));
        Assert.False(await repository.TryRotateAsync(session.Id, owner, "wrong", "new", rememberMe: true, Now));
        Assert.True(await repository.TryRotateAsync(session.Id, owner, "hash", "new", rememberMe: true, Now));
        Assert.Multiple(
            () => Assert.Equal("new", session.RefreshTokenHash),
            () => Assert.True(session.RememberMe),
            () => Assert.Equal(Now, session.LastRotatedAtUtc));
        session.Revoke(Now);
        Assert.False(await repository.TryRotateAsync(session.Id, owner, "new", "next", rememberMe: false, Now));
    }

    private static FoodDiaryDbContext CreateContext() => new(new DbContextOptionsBuilder<FoodDiaryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static UserRefreshTokenSession CreateSession(UserId owner) => UserRefreshTokenSession.Create(
        Guid.NewGuid(), owner, "hash", rememberMe: false, authProvider: "password", ipAddress: null, userAgent: null, Now.AddMinutes(-1));
}
