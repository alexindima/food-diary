using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserSecurityCoverageTests {
    [Fact]
    public void UserSecurityState_RequiringPasswordChange_WithoutPassword_Throws() {
        var state = UserSecurityState.CreateInitial("hash", hasPassword: false);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => state.RequiringPasswordChange());

        Assert.Equal("A password must be set before a password change can be required.", exception.Message);
    }

    [Fact]
    public void UserSecurityState_RequiringPasswordChange_WithPassword_SetsFlag() {
        var state = UserSecurityState.CreateInitial("hash");

        UserSecurityState changed = state.RequiringPasswordChange();

        Assert.True(changed.MustChangePassword);
    }

    [Fact]
    public void UserSecurityAndAdminMethods_CoverNoOpAndUpdatePaths() {
        var user = User.Create("user@example.com", "hash");
        DateTime occurredAtUtc = DateTime.UtcNow;

        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithAuthenticationActivity(occurredAtUtc);
        user.UpdateAdminSecurity(new UserAdminSecurityUpdate(IsEmailConfirmed: null));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: null));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "en"));
        user.UpdateAdminPreferences(new UserAdminPreferenceUpdate(Language: "en"));
        user.UpdateAdminAiQuota(new UserAdminAiQuotaUpdate());
        user.RecordAuthenticationActivity(occurredAtUtc);
        user.UpdatePassword("next-hash");
        user.SetEmailConfirmationToken("email-token", occurredAtUtc.AddHours(1), occurredAtUtc);

        Assert.Multiple(
            () => Assert.Equal(occurredAtUtc, state.LastLoginAtUtc),
            () => Assert.Equal("next-hash", user.Password),
            () => Assert.Equal("email-token", user.EmailConfirmationTokenHash),
            () => Assert.Equal("en", user.Language));
    }
}
