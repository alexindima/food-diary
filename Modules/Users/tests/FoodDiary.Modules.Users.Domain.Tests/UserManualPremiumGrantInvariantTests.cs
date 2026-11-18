using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserManualPremiumGrantInvariantTests {
    private static readonly DateTime Deadline = new(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("too long")]
    public void Grant_RejectsInvalidReasonWithoutChangingAccessOrSecurityVersion(string? invalidReason) {
        var user = User.Create("user@example.test", "hash");
        long version = user.SecurityVersion;
        if (invalidReason is "too long") {
            invalidReason = new string('a', User.ManualPremiumReasonMaxLength + 1);
        }

        Assert.Throws<ArgumentException>("reason", () => user.SetManualPremiumGrant(Deadline, invalidReason));

        Assert.Multiple(
            () => Assert.Null(user.ManualPremiumEndsAtUtc),
            () => Assert.Null(user.ManualPremiumReason),
            () => Assert.Equal(version, user.SecurityVersion));
    }

    [Fact]
    public void Grant_OnlyChangesSessionVersionWhenTheAccessEndChanges() {
        var user = User.Create("user@example.test", "hash");
        long version = user.SecurityVersion;

        user.SetManualPremiumGrant(Deadline, "Support grant");
        long grantedVersion = user.SecurityVersion;
        Assert.True(grantedVersion > version);
        DateTime? modified = user.ModifiedOnUtc;
        user.SetManualPremiumGrant(Deadline, " Support grant ");
        Assert.Equal(modified, user.ModifiedOnUtc);
        user.SetManualPremiumGrant(Deadline, "Corrected reason");

        Assert.Multiple(
            () => Assert.Equal(grantedVersion, user.SecurityVersion),
            () => Assert.Equal(Deadline, user.ManualPremiumEndsAtUtc),
            () => Assert.Equal("Corrected reason", user.ManualPremiumReason));
    }

    [Fact]
    public void RetireTelegramDiary_RejectsAnAccountWithOtherSignInMethods() {
        var user = User.Create("user@example.test", "hash");
        user.LinkTelegram(456);

        Assert.Throws<InvalidOperationException>(() => user.RetireTelegramDiary(Deadline));

        Assert.Multiple(
            () => Assert.Equal(456L, user.TelegramUserId),
            () => Assert.True(user.IsActive),
            () => Assert.Null(user.DeletedAt));
    }
}
