using FoodDiary.Modules.Users.Domain.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserAuthenticationPrivilegesTests {
    [Fact]
    public void UnspecifiedClockCannotGrantTimeDependentPrivileges() {
        DateTime unspecified = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Unspecified);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAuthenticationPrivileges.Evaluate([RoleNames.Support], trialStartedAtUtc: null, trialEndsAtUtc: null, unspecified));

        Assert.Equal("nowUtc", error.ParamName);
    }
}
