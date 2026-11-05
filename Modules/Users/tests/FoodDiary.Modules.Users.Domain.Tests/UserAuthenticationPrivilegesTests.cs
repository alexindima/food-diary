using FoodDiary.Modules.Users.Domain.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserAuthenticationPrivilegesTests {
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void TimedPremiumHasStrictUtcEndAndCapsTokens(int secondsRemaining, bool expectedPremium) {
        DateTime now = new(2026, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        DateTime end = now.AddSeconds(secondsRemaining);
        (IReadOnlyCollection<string> roles, DateTime? cap) = UserAuthenticationPrivileges.Evaluate([RoleNames.Premium, RoleNames.Support],
            trialStartedAtUtc: null, trialEndsAtUtc: null, now, end);
        Assert.Multiple(() => {
            Assert.Equal(expectedPremium, roles.Contains(RoleNames.Premium, StringComparer.Ordinal));
            Assert.Contains(RoleNames.Support, roles, StringComparer.Ordinal);
            Assert.Equal(expectedPremium ? end : null, cap);
        });
    }

    [Fact]
    public void PermanentPremiumAndRemainingTrialKeepTheirOwnExpiryContracts() {
        DateTime now = new(2026, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        (IReadOnlyCollection<string> permanentRoles, DateTime? permanentCap) = UserAuthenticationPrivileges.Evaluate([RoleNames.Premium], trialStartedAtUtc: null, trialEndsAtUtc: null, now);
        (IReadOnlyCollection<string> trialRoles, DateTime? trialCap) = UserAuthenticationPrivileges.Evaluate([RoleNames.Premium], now.AddDays(-1), now.AddDays(1), now, now);
        Assert.Multiple(() => {
            Assert.Contains(RoleNames.Premium, permanentRoles, StringComparer.Ordinal);
            Assert.Null(permanentCap);
            Assert.Contains(RoleNames.Premium, trialRoles, StringComparer.Ordinal);
            Assert.Equal(now.AddDays(1), trialCap);
        });
    }

    [Fact]
    public void UnspecifiedClockCannotGrantTimeDependentPrivileges() {
        DateTime unspecified = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Unspecified);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAuthenticationPrivileges.Evaluate([RoleNames.Support], trialStartedAtUtc: null, trialEndsAtUtc: null, unspecified));

        Assert.Equal("nowUtc", error.ParamName);
    }
}
