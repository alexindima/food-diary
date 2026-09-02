using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserRefreshTokenSessionInvariantTests {
    [Fact]
    public void RefreshTokenSession_Create_WithEmptySessionId_Throws() {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            Guid.Empty,
            UserId.New(),
            "hash",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow));
    }

    [Fact]
    public void RefreshTokenSession_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.Empty,
            "hash",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RefreshTokenSession_Create_WithBlankRefreshTokenHash_Throws(string refreshTokenHash) {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            refreshTokenHash,
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow));
    }

    [Fact]
    public void RefreshTokenSession_Rotate_WhenRevoked_Throws() {
        UserRefreshTokenSession session = CreateRefreshTokenSession();
        session.Revoke(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            session.Rotate("next-hash", rememberMe: true, DateTime.UtcNow.AddMinutes(1), TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void RefreshTokenSession_Revoke_WhenActive_ClearsPreviousTokenState() {
        UserRefreshTokenSession session = CreateRefreshTokenSession();
        DateTime rotatedAtUtc = DateTime.UtcNow.AddMinutes(1);
        session.Rotate("rotated-hash", rememberMe: true, rotatedAtUtc, TimeSpan.FromMinutes(5));
        DateTime revokedAtUtc = rotatedAtUtc.AddMinutes(1);

        session.Revoke(revokedAtUtc);

        Assert.Multiple(
            () => Assert.False(session.IsActive),
            () => Assert.Equal(revokedAtUtc, session.RevokedAtUtc),
            () => Assert.Null(session.PreviousRefreshTokenHash),
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Equal(revokedAtUtc, session.ModifiedOnUtc));
    }

    [Fact]
    public void RefreshTokenSession_Revoke_WhenAlreadyRevoked_DoesNotChangeTimestamp() {
        UserRefreshTokenSession session = CreateRefreshTokenSession();
        DateTime revokedAtUtc = DateTime.UtcNow.AddMinutes(1);
        session.Revoke(revokedAtUtc);

        session.Revoke(revokedAtUtc.AddMinutes(5));

        Assert.Equal(revokedAtUtc, session.RevokedAtUtc);
        Assert.Equal(revokedAtUtc, session.ModifiedOnUtc);
    }

    private static UserRefreshTokenSession CreateRefreshTokenSession() =>
        UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "refresh-hash",
            rememberMe: false,
            authProvider: "local",
            ipAddress: "127.0.0.1",
            userAgent: "test",
            DateTime.UtcNow);
}
