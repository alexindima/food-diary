using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class PrivilegedSessionBoundaryTests {
    [Theory]
    [InlineData("missing")]
    [InlineData("owner")]
    [InlineData("revoked")]
    [InlineData("unbounded")]
    [InlineData("expired")]
    public async Task UnusableSessionsCannotConfirmPrivilegedAccess(string scenario) {
        DateTime now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        var user = UserId.New();
        DateTime? expiresAtUtc = scenario switch {
            "unbounded" => null,
            "expired" => now,
            _ => now.AddHours(1),
        };
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), scenario is "owner" ? UserId.New() : user,
            "proof", rememberMe: false, "password", ipAddress: null, userAgent: null, now.AddMinutes(-1),
            expiresAtUtc);
        if (scenario is "revoked") {
            session.Revoke(now);
        }
        IRefreshTokenSessionReadRepository sessions = Substitute.For<IRefreshTokenSessionReadRepository>();
        sessions.GetByIdAsync(session.Id, CancellationToken.None).Returns(scenario is "missing" ? null : session);

        bool confirmed = await new PrivilegedSessionService(sessions, new FixedClock(now))
            .IsConfirmedAsync(user, session.Id.Value, allowAdminSession: true, CancellationToken.None);

        Assert.False(confirmed);
        await sessions.Received(1).GetByIdAsync(session.Id, CancellationToken.None);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task AdministrativeConfirmationRequiresPermissionAndAnUnexpiredGrant(bool allowAdminSession, bool expired, bool expected) {
        DateTime now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        var user = UserId.New();
        var session = UserRefreshTokenSession.Create(RefreshTokenSessionId.New(), user, "proof", rememberMe: false,
            "admin-sso", ipAddress: null, userAgent: null, now.AddMinutes(-10), now.AddHours(1));
        session.GrantAdminAccess(expired ? now : now.AddMinutes(5));
        IRefreshTokenSessionReadRepository sessions = Substitute.For<IRefreshTokenSessionReadRepository>();
        sessions.GetByIdAsync(session.Id, CancellationToken.None).Returns(session);

        bool confirmed = await new PrivilegedSessionService(sessions, new FixedClock(now))
            .IsConfirmedAsync(user, session.Id.Value, allowAdminSession, CancellationToken.None);

        Assert.Equal(expected, confirmed);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
