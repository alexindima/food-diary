using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RefreshTokenChronologyTests {
    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RefreshTokenSession_RejectsTimestampsBeforeLastRotationAtomically() {
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "initial-hash",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.Rotate("rewound-hash", rememberMe: true, Now.AddTicks(-1), TimeSpan.FromMinutes(5)));
        Assert.Equal("initial-hash", session.RefreshTokenHash);
        Assert.Equal(Now, session.LastRotatedAtUtc);

        session.Rotate("rotated-hash", rememberMe: true, Now.AddMinutes(1), TimeSpan.FromMinutes(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Revoke(Now));
        Assert.True(session.IsActive);
        Assert.Null(session.RevokedAtUtc);
    }
}
