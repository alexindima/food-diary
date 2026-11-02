using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;

namespace FoodDiary.Modules.Marketing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingConsentSnapshotTests {
    [Fact]
    public void Attribution_PreservesTheConsentEvidenceThatAuthorizedItsCollection() {
        DateTime granted = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var attribution = MarketingAttributionEvent.Create("page_landing", granted.AddMinutes(1), userId: null,
            new AnonymousVisitorId("owned-visitor"), new MarketingSessionId("owned-session"), "/",
            consentNoticeVersion: $" {PrivacyNoticeVersions.Current} ", consentGrantedAtUtc: granted);

        Assert.Multiple(
            () => Assert.Equal(PrivacyNoticeVersions.Current, attribution.ConsentNoticeVersion),
            () => Assert.Equal(granted, attribution.ConsentGrantedAtUtc));
    }
}
