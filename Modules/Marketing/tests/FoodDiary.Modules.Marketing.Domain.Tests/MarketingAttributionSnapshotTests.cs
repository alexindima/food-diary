using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using FoodDiary.Modules.Marketing.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Marketing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionSnapshotTests {
    [Fact]
    public void AnonymousAttribution_NormalizesEveryCampaignFieldAndPreservesTheSuppliedEventIdentity() {
        var visitorTemplate = new AnonymousVisitorId("original-visitor");
        var sessionTemplate = new MarketingSessionId("original-session");
        AnonymousVisitorId visitor = visitorTemplate with { Value = "  visitor/from-local-storage  " };
        MarketingSessionId session = sessionTemplate with { Value = "  session:opaque  " };
        var eventId = new MarketingAttributionEventId(Guid.Parse("12345678-1234-5678-9abc-def012345678"));
        MarketingAttributionEventId replacement = eventId with { Value = Guid.NewGuid() };
        var occurredAt = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        var attribution = MarketingAttributionEvent.Create(" page_landing ", occurredAt, userId: null, visitor, session,
            " /pricing?plan=annual ", " search.example.test ", " source ", " medium ", " campaign ", " content ", " term ", " v1.2.3 ", eventId.Value);

        Assert.Multiple(
            () => Assert.Equal(eventId, attribution.Id),
            () => Assert.NotEqual(eventId, replacement),
            () => Assert.Equal(occurredAt, attribution.OccurredAtUtc),
            () => Assert.Null(attribution.UserId),
            () => Assert.Equal("page_landing", attribution.EventType),
            () => Assert.Equal("visitor/from-local-storage", attribution.AnonymousId.ToString()),
            () => Assert.Equal("session:opaque", attribution.SessionId.ToString()),
            () => Assert.Equal("/pricing?plan=annual", attribution.LandingPath),
            () => Assert.Equal("search.example.test", attribution.ReferrerHost),
            () => Assert.Equal("source", attribution.UtmSource),
            () => Assert.Equal("medium", attribution.UtmMedium),
            () => Assert.Equal("campaign", attribution.UtmCampaign),
            () => Assert.Equal("content", attribution.UtmContent),
            () => Assert.Equal("term", attribution.UtmTerm),
            () => Assert.Equal("v1.2.3", attribution.BuildVersion),
            () => Assert.Equal("original-visitor", visitorTemplate.Value),
            () => Assert.Equal("original-session", sessionTemplate.Value),
            () => Assert.Equal("12345678-1234-5678-9abc-def012345678", eventId.ToString()));
    }
}
