using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;

namespace FoodDiary.Modules.Marketing.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionEventInvariantTests {
    [Fact]
    public void Create_NormalizesOpaqueVisitorAndSessionWithoutRequiringUuids() {
        var attributionEvent = MarketingAttributionEvent.Create("custom_event", DateTime.UtcNow, userId: null,
            new AnonymousVisitorId("  visitor/from-local-storage  "), new MarketingSessionId("  session:opaque  "), "/");

        Assert.Multiple(
            () => Assert.Equal("visitor/from-local-storage", attributionEvent.AnonymousId.Value),
            () => Assert.Equal("session:opaque", attributionEvent.SessionId.Value),
            () => Assert.Equal("custom_event", attributionEvent.EventType));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_PreservesTheNinetySixCharacterBoundary(bool oversizedSession) {
        var anonymous = new AnonymousVisitorId(new string('a', oversizedSession ? 96 : 97));
        var session = new MarketingSessionId(new string('s', oversizedSession ? 97 : 96));
        Assert.Throws<ArgumentOutOfRangeException>(() => MarketingAttributionEvent.Create("custom_event", DateTime.UtcNow,
            userId: null, anonymous, session, "/"));
    }

    [Fact]
    public void Create_PreservesRequiredIdentityValidationOrder() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => MarketingAttributionEvent.Create("custom_event", DateTime.UtcNow,
            userId: null, AnonymousVisitorId.Empty, MarketingSessionId.Empty, "/"));
        Assert.Equal("anonymousId", error.ParamName);
    }

    [Fact]
    public void Create_WithEventId_UsesItAsTheDurableEntityIdentity() {
        var eventId = Guid.NewGuid();

        var attributionEvent = MarketingAttributionEvent.Create(
            "page_landing",
            new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
            userId: null,
            new AnonymousVisitorId("anon-1"),
            new MarketingSessionId("session-1"),
            "/",
            eventId: eventId);

        Assert.Equal(eventId, attributionEvent.Id.Value);
    }

    [Fact]
    public void Create_WithTooLongUtmValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarketingAttributionEvent.Create(
            "page_landing",
            new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
            userId: null,
            new AnonymousVisitorId("anon-1"),
            new MarketingSessionId("session-1"),
            "/",
            utmSource: new string('a', 161)));
    }
}
