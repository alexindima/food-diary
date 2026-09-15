using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RefreshTokenRotationAtomicityTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RefreshTokenRotation_WhenValidationFails_IsAtomic() {
        var session = UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            "original-hash",
            rememberMe: true,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now);

        Assert.Throws<ArgumentException>(() =>
            session.Rotate(" ", rememberMe: false, Now.AddMinutes(1), TimeSpan.FromMinutes(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            session.Rotate("next-hash", rememberMe: false, DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), TimeSpan.FromTicks(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => UserRefreshTokenSession.Create(
            Guid.NewGuid(),
            UserId.New(),
            new string('h', 513),
            rememberMe: false,
            authProvider: null,
            ipAddress: null,
            userAgent: null,
            Now));

        Assert.Multiple(
            () => Assert.Equal("original-hash", session.RefreshTokenHash),
            () => Assert.True(session.RememberMe),
            () => Assert.Equal(Now, session.LastRotatedAtUtc),
            () => Assert.Null(session.PreviousRefreshTokenHash),
            () => Assert.Null(session.PreviousRefreshTokenValidUntilUtc),
            () => Assert.Null(session.ModifiedOnUtc));
    }
}
