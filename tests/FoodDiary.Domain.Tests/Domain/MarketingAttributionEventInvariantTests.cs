using FoodDiary.Domain.Entities.Tracking;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class MarketingAttributionEventInvariantTests {
    [Fact]
    public void Create_WithTooLongUtmValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarketingAttributionEvent.Create(
            "page_landing",
            new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
            userId: null,
            "anon-1",
            "session-1",
            "/",
            utmSource: new string('a', 161)));
    }
}
