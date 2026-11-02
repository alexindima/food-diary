using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserPrivacyConsentBoundaryTests {
    public static TheoryData<string> InvalidWithdrawalNotices => ["", " ", new string('n', 65)];

    [Theory]
    [MemberData(nameof(InvalidWithdrawalNotices))]
    public void InvalidWithdrawalNotice_DoesNotChangeConsentOrAppendHistory(string notice) {
        var user = User.Create("privacy-boundary@example.test", "synthetic-hash");
        DateTime granted = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: true, PrivacyNoticeVersions.Current, granted);
        DateTime? modified = user.ModifiedOnUtc;

        Assert.Throws<ArgumentException>(() => user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking,
            granted: false, notice, granted.AddMinutes(1)));

        Assert.Multiple(
            () => Assert.Equal(granted, user.HealthConsentAcceptedAtUtc),
            () => Assert.Null(user.HealthConsentDeclinedAtUtc),
            () => Assert.Equal(PrivacyNoticeVersions.Current, user.HealthConsentNoticeVersion),
            () => Assert.Equal(modified, user.ModifiedOnUtc));
        Assert.True(Assert.Single(user.PrivacyConsentEvents).Granted);
    }
}
