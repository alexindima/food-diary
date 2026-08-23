using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserSecurityVersionTests {
    [Fact]
    public void SecuritySensitiveMutations_AdvanceSecurityVersion() {
        var user = User.Create("security-version@example.com", "initial-hash");
        var adminRole = Role.Create("Admin");

        Assert.Equal(0, user.SecurityVersion);

        user.UpdatePassword("updated-hash");
        Assert.Equal(1, user.SecurityVersion);

        user.ReplaceRoles([adminRole]);
        Assert.Equal(2, user.SecurityVersion);

        user.Deactivate();
        Assert.Equal(3, user.SecurityVersion);
    }

    [Fact]
    public void IdempotentRoleAndLifecycleMutations_DoNotAdvanceSecurityVersion() {
        var user = User.Create("security-idempotency@example.com", "initial-hash");
        var adminRole = Role.Create("Admin");
        user.ReplaceRoles([adminRole]);
        user.ReplaceRoles([adminRole]);
        long versionAfterRoleAssignment = user.SecurityVersion;

        user.Deactivate();
        user.Deactivate();

        Assert.Equal(1, versionAfterRoleAssignment);
        Assert.Equal(2, user.SecurityVersion);
    }

    [Fact]
    public void CompletePasswordReset_AdvancesSecurityVersion() {
        var user = User.Create("security-reset@example.com", "initial-hash");

        user.CompletePasswordReset("reset-hash");

        Assert.Equal(1, user.SecurityVersion);
    }
}
