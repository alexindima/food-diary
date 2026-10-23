using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserRefreshTokenSessionInvariantTests {
    [Fact]
    public void RefreshSessionExpiration_IsPreservedOnCreationAndReplacedOnRotation() {
        var now = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), UserId.New(), "initial",
            rememberMe: false, authProvider: "password", ipAddress: null, userAgent: null, now, now.AddDays(30));
        Assert.Equal(now.AddDays(30), session.ExpiresAtUtc);

        session.Rotate("next", rememberMe: true, now.AddMinutes(1), TimeSpan.Zero, now.AddDays(90));

        Assert.Equal(now.AddDays(90), session.ExpiresAtUtc);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Rotate("invalid", rememberMe: true, now.AddMinutes(2), TimeSpan.Zero, now));
        Assert.Equal("next", session.RefreshTokenHash);
    }

    [Fact]
    public void Rotation_PreservesTheOriginalNormalizedClientMetadata() {
        var id = RefreshTokenSessionId.New();
        var userId = UserId.New();
        var created = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
        var session = UserRefreshTokenSession.Create(id, userId, " original-hash ", rememberMe: false,
            authProvider: " local ", ipAddress: " 2001:db8::1 ", userAgent: " fixture-client ", created);

        session.Rotate("new-hash", rememberMe: true, created.AddMinutes(1), TimeSpan.FromMinutes(5));

        Assert.Multiple(
            () => Assert.Equal(id, session.Id),
            () => Assert.Equal(userId, session.UserId),
            () => Assert.Equal("local", session.AuthProvider),
            () => Assert.Equal("2001:db8::1", session.IpAddress),
            () => Assert.Equal("fixture-client", session.UserAgent),
            () => Assert.Equal(created, session.CreatedAtUtc),
            () => Assert.Equal("original-hash", session.PreviousRefreshTokenHash),
            () => Assert.Equal("new-hash", session.RefreshTokenHash),
            () => Assert.True(session.RememberMe));
    }

    [Fact]
    public void RefreshTokenSession_Create_WithEmptySessionId_Throws() {
        Assert.Throws<ArgumentException>(() => UserRefreshTokenSession.Create(
            RefreshTokenSessionId.Empty,
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
            RefreshTokenSessionId.New(),
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
            RefreshTokenSessionId.New(),
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
            RefreshTokenSessionId.New(),
            UserId.New(),
            "refresh-hash",
            rememberMe: false,
            authProvider: "local",
            ipAddress: "127.0.0.1",
            userAgent: "test",
            DateTime.UtcNow);
}
