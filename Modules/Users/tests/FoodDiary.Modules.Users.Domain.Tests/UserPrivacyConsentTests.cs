using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserPrivacyConsentTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UndefinedPurpose_DoesNotChangeConsentOrCreateAnEvent(bool granted) {
        var user = User.Create("privacy@example.test", "hash");
        DateTime? modified = user.ModifiedOnUtc;

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.RecordPrivacyConsent((PrivacyConsentPurpose)int.MaxValue, granted, PrivacyNoticeVersions.Current,
                new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc)));

        Assert.Multiple(
            () => Assert.Equal("purpose", error.ParamName),
            () => Assert.Empty(user.PrivacyConsentEvents),
            () => Assert.Null(user.HealthConsentAcceptedAtUtc),
            () => Assert.Null(user.HealthConsentDeclinedAtUtc),
            () => Assert.Null(user.MarketingConsentAcceptedAtUtc),
            () => Assert.Null(user.AiConsentAcceptedAt),
            () => Assert.Equal(modified, user.ModifiedOnUtc));
    }

    [Fact]
    public void GrantAndWithdrawal_PreserveBothDecisionsAndTheirNotice() {
        var user = User.Create("privacy@example.test", "hash");
        DateTime grant = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: true, PrivacyNoticeVersions.Current, grant);
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: false, PrivacyNoticeVersions.Current, grant.AddMinutes(1));
        Assert.Null(user.HealthConsentAcceptedAtUtc);
        Assert.Collection(user.PrivacyConsentEvents,
            entry => { Assert.True(entry.Granted); Assert.Equal(grant, entry.OccurredAtUtc); },
            entry => { Assert.False(entry.Granted); Assert.Equal(grant.AddMinutes(1), entry.OccurredAtUtc); });
        Assert.All(user.PrivacyConsentEvents, entry => Assert.Equal(PrivacyNoticeVersions.Current, entry.NoticeVersion));
    }

    [Fact]
    public void StaleNotice_DoesNotChangeConsentOrCreateAnEvent() {
        var user = User.Create("privacy@example.test", "hash");
        Assert.Throws<ArgumentException>(() => user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: true, "old", DateTime.UtcNow));
        Assert.Null(user.HealthConsentAcceptedAtUtc);
        Assert.Empty(user.PrivacyConsentEvents);
    }

    [Fact]
    public void Withdrawal_WithAnOlderNotice_RemainsAvailable() {
        var user = User.Create("privacy@example.test", "hash");
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: true, PrivacyNoticeVersions.Current, DateTime.UtcNow);
        user.RecordPrivacyConsent(PrivacyConsentPurpose.HealthTracking, granted: false, "older-notice", DateTime.UtcNow);
        Assert.Null(user.HealthConsentAcceptedAtUtc);
        Assert.Equal("older-notice", user.PrivacyConsentEvents.Last().NoticeVersion);
    }

    [Fact]
    public void ExistingAiConsentPath_DoesNotInventVersionedHistory() {
        var user = User.Create("privacy@example.test", "hash");
        user.AcceptAiConsent();
        user.RevokeAiConsent();
        Assert.Equal(2, user.PrivacyConsentEvents.Count);
        Assert.All(user.PrivacyConsentEvents, entry => Assert.Equal(PrivacyNoticeVersions.LegacyUnversioned, entry.NoticeVersion));
    }
}
