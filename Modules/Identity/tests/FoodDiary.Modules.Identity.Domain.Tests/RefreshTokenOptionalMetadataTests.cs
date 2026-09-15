using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RefreshTokenOptionalMetadataTests {
    [Fact]
    public void MiscDomainMethods_CoverRemainingBranches() {
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            " refresh ",
            rememberMe: true,
            authProvider: " local ",
            ipAddress: " 127.0.0.1 ",
            userAgent: new string('a', 600),
            DateTime.UtcNow);
        session.Rotate(" next-refresh ", rememberMe: false, DateTime.UtcNow.AddMinutes(1), TimeSpan.Zero);
        var sessionWithNullOptionals = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "refresh",
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            DateTime.UtcNow);
        Assert.Multiple(
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Null(sessionWithNullOptionals.AuthProvider));
    }
}
