using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionStateGuardTests {
    [Fact]
    public void RevokedOrAlreadyConvertedSession_CannotBeConvertedAgain() {
        DateTime now = DateTime.UtcNow;
        var revoked = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), UserId.New(), "original", rememberMe: false, authProvider: null, ipAddress: null, userAgent: null, now, now.AddDays(1));
        revoked.Revoke(now);
        var converted = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), UserId.New(), "original", rememberMe: false, authProvider: null, ipAddress: null, userAgent: null, now, now.AddDays(1));
        converted.ConvertToBrowserSession("browser", 7, now);

        Assert.Throws<InvalidOperationException>(() => revoked.ConvertToBrowserSession("replacement", 9, now));
        Assert.Throws<InvalidOperationException>(() => converted.ConvertToBrowserSession("replacement", 9, now));
        Assert.Multiple(
            () => Assert.Null(revoked.BrowserSecurityVersion),
            () => Assert.Equal("original", revoked.RefreshTokenHash),
            () => Assert.Equal(7, converted.BrowserSecurityVersion),
            () => Assert.Equal("browser", converted.RefreshTokenHash));
    }

    [Fact]
    public void Revalidation_RequiresAnActiveBrowserSessionAndANonNegativeVersion() {
        DateTime now = DateTime.UtcNow;
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), UserId.New(), "original", rememberMe: false, authProvider: null, ipAddress: null, userAgent: null, now, now.AddDays(1));
        Assert.Throws<InvalidOperationException>(() => session.RefreshBrowserSession(7, now));
        Assert.Throws<InvalidOperationException>(() => session.ConvertToBrowserSession("browser", -1, now));
        session.ConvertToBrowserSession("browser", 7, now);
        Assert.Throws<InvalidOperationException>(() => session.RefreshBrowserSession(-1, now));
        session.Revoke(now);
        Assert.Throws<InvalidOperationException>(() => session.RefreshBrowserSession(9, now));
        Assert.Equal(7, session.BrowserSecurityVersion);
    }
}
